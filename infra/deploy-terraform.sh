#!/bin/bash

# HouseHeroes Azure Infrastructure Deployment Script (Terraform)
# Usage: ./deploy-terraform.sh <environment> [resource-group-name]

set -e

ENVIRONMENT=${1:-staging}
RESOURCE_GROUP=${2:-"rg-househeroes-$ENVIRONMENT"}
LOCATION="Canada Central"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TF_VAR_FILE="${ENVIRONMENT}.tfvars"

echo "Deploying HouseHeroes infrastructure with Terraform for environment: $ENVIRONMENT"
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo "Subscription: $SUBSCRIPTION_ID"
echo "Terraform vars file: $TF_VAR_FILE"

# Check if terraform is installed
if ! command -v terraform &> /dev/null; then
    echo "Error: Terraform is not installed. Please install Terraform first."
    echo "Visit: https://developer.hashicorp.com/terraform/tutorials/aws-get-started/install-cli"
    exit 1
fi

# Check if resource group exists, create if it doesn't
if ! az group show --name "$RESOURCE_GROUP" &>/dev/null; then
    echo "Creating resource group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
    echo "Resource group $RESOURCE_GROUP already exists"
fi

# Check if tfvars file exists
if [ ! -f "$TF_VAR_FILE" ]; then
    echo "Error: Terraform variables file '$TF_VAR_FILE' not found."
    echo "Please create it based on terraform.tfvars.example"
    exit 1
fi

# Initialize Terraform
echo "Initializing Terraform..."
terraform init

# Validate Terraform configuration
echo "Validating Terraform configuration..."
terraform validate

# Plan the deployment
echo "Planning Terraform deployment..."
terraform plan \
    -var-file="$TF_VAR_FILE" \
    -var="resource_group_name=$RESOURCE_GROUP" \
    -out=tfplan

# Prompt for confirmation unless --auto-approve is passed
if [[ "$*" != *"--auto-approve"* ]]; then
    echo ""
    echo "Review the plan above. Do you want to apply these changes? (y/N)"
    read -r response
    if [[ ! "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        echo "Deployment cancelled."
        exit 0
    fi
fi

# Apply the Terraform plan
echo "Applying Terraform configuration..."
terraform apply tfplan

# Get outputs
echo "Retrieving Terraform outputs..."
CONTAINER_APP_URL=$(terraform output -raw container_app_url)
ACR_LOGIN_SERVER=$(terraform output -raw container_registry_login_server)
ACR_NAME=$(terraform output -raw container_registry_name)
ACR_USERNAME=$(terraform output -raw container_registry_admin_username)
ACR_PASSWORD=$(terraform output -raw container_registry_admin_password)
SQL_SERVER=$(terraform output -raw sql_server_name)
SQL_SERVER_FQDN=$(terraform output -raw sql_server_fqdn)
DATABASE_CONNECTION_STRING=$(terraform output -raw database_connection_string)
APP_INSIGHTS_KEY=$(terraform output -raw application_insights_instrumentation_key)
APP_INSIGHTS_CONNECTION_STRING=$(terraform output -raw application_insights_connection_string)

# Build and push initial Docker image
echo "Building and pushing initial Docker image..."
ACR_LOGIN_SERVER_CLEAN=$(echo "$ACR_LOGIN_SERVER" | tr -d '"')

# Login to ACR
az acr login --name "$ACR_NAME"

# Build and push the Docker image from the parent directory
cd ..
docker build -t "$ACR_LOGIN_SERVER_CLEAN/househeroes-api:latest" .
docker push "$ACR_LOGIN_SERVER_CLEAN/househeroes-api:latest"

# Update Container App with the actual image
echo "Updating Container App with the actual Docker image..."
az containerapp update \
    --name "househeroes-api-$ENVIRONMENT" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$ACR_LOGIN_SERVER_CLEAN/househeroes-api:latest"

# Update Migration Job with the actual image
echo "Updating Migration Job with the actual Docker image..."
az containerapp job update \
    --name "househeroes-migration-job" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$ACR_LOGIN_SERVER_CLEAN/househeroes-api:latest" \
    --command "dotnet" "ef" "database" "update" \
    --replica-timeout 1800

cd infra

echo ""
echo "=== Deployment Complete ==="
echo "Container App URL: $CONTAINER_APP_URL"
echo "Container Registry: $ACR_LOGIN_SERVER"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Application Insights Key: $APP_INSIGHTS_KEY"
echo ""
echo "=== GitHub Secrets to Configure ==="
echo "AZURE_CREDENTIALS_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): Create a service principal with contributor access"
echo "AZURE_RESOURCE_GROUP_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): $RESOURCE_GROUP"
echo "ACR_USERNAME: $ACR_USERNAME"
echo "ACR_PASSWORD: $ACR_PASSWORD"
echo "DATABASE_CONNECTION_STRING_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): $DATABASE_CONNECTION_STRING"
echo ""

# Create service principal for GitHub Actions
echo "Creating service principal for GitHub Actions..."
SP_NAME="sp-househeroes-github-$ENVIRONMENT"
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

SP_JSON=$(az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role contributor \
    --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" \
    --sdk-auth)

echo ""
echo "=== GitHub Secret AZURE_CREDENTIALS_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]') ==="
echo "$SP_JSON"
echo ""
echo "Add this JSON as a secret in your GitHub repository settings."
echo ""
echo "=== Terraform State ==="
echo "Terraform state is stored locally. Consider using remote state for production."
echo "To destroy this infrastructure later, run: terraform destroy -var-file='$TF_VAR_FILE' -var='resource_group_name=$RESOURCE_GROUP'"