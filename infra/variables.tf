variable "environment" {
  description = "The environment name (staging, production)"
  type        = string
  default     = "staging"

  validation {
    condition     = contains(["staging", "production"], var.environment)
    error_message = "Environment must be either 'staging' or 'production'."
  }
}

variable "location" {
  description = "The Azure region where all resources will be created"
  type        = string
  default     = "Canada Central"
}

variable "app_name" {
  description = "The application name"
  type        = string
  default     = "househeroes"
}

variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
}

variable "sql_server_admin_login" {
  description = "The SQL Server administrator login"
  type        = string
  sensitive   = true
}

variable "sql_server_admin_password" {
  description = "The SQL Server administrator password (if not provided, will be randomly generated)"
  type        = string
  default     = null
  sensitive   = true

  validation {
    condition = var.sql_server_admin_password == null || (
      length(var.sql_server_admin_password) >= 8 &&
      can(regex("[A-Z]", var.sql_server_admin_password)) &&
      can(regex("[a-z]", var.sql_server_admin_password)) &&
      can(regex("[0-9]", var.sql_server_admin_password)) &&
      can(regex("[^A-Za-z0-9]", var.sql_server_admin_password))
    )
    error_message = "SQL Server password must be at least 8 characters long and contain uppercase, lowercase, numeric, and special characters."
  }
}

variable "tags" {
  description = "A mapping of tags to assign to all resources"
  type        = map(string)
  default = {
    Project     = "HouseHeroes"
    ManagedBy   = "Terraform"
  }
}