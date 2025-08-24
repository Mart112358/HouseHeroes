environment             = "staging"
location               = "Canada Central"
app_name              = "househeroes"
resource_group_name   = "rg-househeroes-staging"
sql_server_admin_login = "hhadmin"

tags = {
  Project     = "HouseHeroes"
  Environment = "staging"
  ManagedBy   = "Terraform"
}