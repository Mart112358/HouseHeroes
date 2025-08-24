environment             = "production"
location               = "Canada Central"
app_name              = "househeroes"
resource_group_name   = "rg-househeroes-production"
sql_server_admin_login = "hhadmin"

tags = {
  Project     = "HouseHeroes"
  Environment = "production"
  ManagedBy   = "Terraform"
}