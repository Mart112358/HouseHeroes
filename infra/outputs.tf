output "container_app_url" {
  description = "The URL of the container app"
  value       = "https://${azurerm_container_app.main.latest_revision_fqdn}"
}

output "container_registry_login_server" {
  description = "The login server URL for the container registry"
  value       = azurerm_container_registry.main.login_server
}

output "container_registry_name" {
  description = "The name of the container registry"
  value       = azurerm_container_registry.main.name
}

output "container_registry_admin_username" {
  description = "The admin username for the container registry"
  value       = azurerm_container_registry.main.admin_username
  sensitive   = true
}

output "container_registry_admin_password" {
  description = "The admin password for the container registry"
  value       = azurerm_container_registry.main.admin_password
  sensitive   = true
}

output "sql_server_name" {
  description = "The name of the SQL server"
  value       = azurerm_mssql_server.main.name
}

output "sql_server_fqdn" {
  description = "The fully qualified domain name of the SQL server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_server_admin_password" {
  description = "The SQL Server administrator password (only if generated)"
  value       = var.sql_server_admin_password == null ? random_password.sql_admin_password[0].result : null
  sensitive   = true
}

output "database_connection_string" {
  description = "The connection string for the SQL Server database"
  value       = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=househeroes;User Id=${var.sql_server_admin_login};Password=${coalesce(var.sql_server_admin_password, try(random_password.sql_admin_password[0].result, ""))};Encrypt=true;TrustServerCertificate=false;"
  sensitive   = true
}

output "application_insights_instrumentation_key" {
  description = "The instrumentation key for Application Insights"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "The connection string for Application Insights"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

output "resource_group_name" {
  description = "The name of the resource group"
  value       = data.azurerm_resource_group.main.name
}

output "resource_group_location" {
  description = "The location of the resource group"
  value       = data.azurerm_resource_group.main.location
}