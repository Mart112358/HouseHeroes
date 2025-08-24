terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.1"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Data sources
data "azurerm_client_config" "current" {}
data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

# Local variables
locals {
  container_app_environment_name = "${var.app_name}-env-${var.environment}"
  container_app_name             = "${var.app_name}-api-${var.environment}"
  sql_server_name                = "${var.app_name}-db-${var.environment}"
  log_analytics_workspace_name   = "${var.app_name}-logs-${var.environment}"
  application_insights_name      = "${var.app_name}-insights-${var.environment}"
  container_registry_name        = "${var.app_name}acr${var.environment}"
}

# Random password for SQL Server admin (if not provided)
resource "random_password" "sql_admin_password" {
  count   = var.sql_server_admin_password == null ? 1 : 0
  length  = 32
  special = true
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "main" {
  name                = local.log_analytics_workspace_name
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 90

  tags = var.tags
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = local.application_insights_name
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.main.id

  tags = var.tags
}

# Container Registry
resource "azurerm_container_registry" "main" {
  name                = local.container_registry_name
  resource_group_name = data.azurerm_resource_group.main.name
  location            = var.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = var.tags
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = local.sql_server_name
  resource_group_name          = data.azurerm_resource_group.main.name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_server_admin_login
  administrator_login_password = coalesce(var.sql_server_admin_password, try(random_password.sql_admin_password[0].result, ""))
  minimum_tls_version          = "1.2"
  public_network_access_enabled = true

  tags = var.tags
}

# SQL Server Database
resource "azurerm_mssql_database" "main" {
  name           = "househeroes"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic"
  zone_redundant = false

  tags = var.tags
}

# SQL Server Firewall Rule to allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Container App Environment
resource "azurerm_container_app_environment" "main" {
  name                       = local.container_app_environment_name
  location                   = var.location
  resource_group_name        = data.azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  tags = var.tags
}

# Container App
resource "azurerm_container_app" "main" {
  name                         = local.container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = data.azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    min_replicas = var.environment == "production" ? 2 : 1
    max_replicas = var.environment == "production" ? 10 : 3

    container {
      name   = "househeroes-api"
      image  = "mcr.microsoft.com/dotnet/samples:aspnetapp" # Placeholder image for initial deployment
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "production" ? "Production" : "Staging"
      }

      env {
        name        = "ConnectionStrings__househeroes"
        secret_name = "connection-string"
      }

      env {
        name        = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        secret_name = "app-insights-connection-string"
      }
    }

    http_scale_rule {
      name                = "http-scaling-rule"
      concurrent_requests = "30"
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "connection-string"
    value = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=househeroes;User Id=${var.sql_server_admin_login};Password=${coalesce(var.sql_server_admin_password, try(random_password.sql_admin_password[0].result, ""))};Encrypt=true;TrustServerCertificate=false;"
  }

  secret {
    name  = "app-insights-connection-string"
    value = azurerm_application_insights.main.connection_string
  }

  tags = var.tags
}

# Migration Job Container App
resource "azurerm_container_app_job" "migration" {
  name                         = "${var.app_name}-migration-job"
  location                     = var.location
  resource_group_name          = data.azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id

  replica_timeout_in_seconds = 1800
  replica_retry_limit        = 3
  
  manual_trigger_config {
    parallelism              = 1
    replica_completion_count = 1
  }

  template {
    container {
      name    = "migration"
      image   = "mcr.microsoft.com/dotnet/runtime:9.0" # Placeholder image for initial deployment
      cpu     = 0.5
      memory  = "1Gi"
      command = ["sleep", "30"] # Placeholder command

      env {
        name        = "ConnectionStrings__househeroes"
        secret_name = "connection-string"
      }
    }
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "connection-string"
    value = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=househeroes;User Id=${var.sql_server_admin_login};Password=${coalesce(var.sql_server_admin_password, try(random_password.sql_admin_password[0].result, ""))};Encrypt=true;TrustServerCertificate=false;"
  }

  tags = var.tags
}