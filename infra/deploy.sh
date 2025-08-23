#!/bin/bash

# HouseHeroes Azure Infrastructure Deployment Script
# Usage: ./deploy.sh <environment> [resource-group-name]

set -e

ENVIRONMENT=${1:-staging}
RESOURCE_GROUP=${2:-"rg-househeroes-$ENVIRONMENT"}
LOCATION="Canada Central"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo "Deploying HouseHeroes infrastructure for environment: $ENVIRONMENT"
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo "Subscription: $SUBSCRIPTION_ID"

# Check if resource group exists, create if it doesn't
if ! az group show --name "$RESOURCE_GROUP" &>/dev/null; then
    echo "Creating resource group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
    echo "Resource group $RESOURCE_GROUP already exists"
fi

# Generate random passwords if not set
if [ -z "$POSTGRES_ADMIN_LOGIN" ]; then
    POSTGRES_ADMIN_LOGIN="hhadmin"
fi

if [ -z "$POSTGRES_ADMIN_PASSWORD" ]; then
    POSTGRES_ADMIN_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    echo "Generated PostgreSQL password (save this securely): $POSTGRES_ADMIN_PASSWORD"
fi

# Deploy the infrastructure
echo "Deploying Bicep template..."
DEPLOYMENT_NAME="househeroes-infra-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --template-file main.bicep \
    --parameters \
        environment="$ENVIRONMENT" \
        location="$LOCATION" \
        postgresAdminLogin="$POSTGRES_ADMIN_LOGIN" \
        postgresAdminPassword="$POSTGRES_ADMIN_PASSWORD"

# Build and push initial Docker image
echo "Building and pushing initial Docker image..."
ACR_NAME="househeroesacr$ENVIRONMENT"
ACR_LOGIN_SERVER="$ACR_NAME.azurecr.io"

# Login to ACR
az acr login --name "$ACR_NAME"

# Build and push the Docker image from the parent directory
cd ..
docker build -t "$ACR_LOGIN_SERVER/househeroes-api:latest" .
docker push "$ACR_LOGIN_SERVER/househeroes-api:latest"

# Update Container App with the actual image
echo "Updating Container App with the actual Docker image..."
az containerapp update \
    --name "househeroes-api-$ENVIRONMENT" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$ACR_LOGIN_SERVER/househeroes-api:latest"

# Update Migration Job with the actual image
echo "Updating Migration Job with the actual Docker image..."
az containerapp job update \
    --name "househeroes-migration-job" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$ACR_LOGIN_SERVER/househeroes-api:latest" \
    --command "dotnet" "ef" "database" "update" \
    --replica-timeout 1800

cd infra

# Get outputs
echo "Retrieving deployment outputs..."
OUTPUTS=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query properties.outputs)

CONTAINER_APP_URL=$(echo "$OUTPUTS" | jq -r .containerAppUrl.value)
ACR_LOGIN_SERVER=$(echo "$OUTPUTS" | jq -r .containerRegistryLoginServer.value)
ACR_NAME=$(echo "$OUTPUTS" | jq -r .containerRegistryName.value)
POSTGRES_SERVER=$(echo "$OUTPUTS" | jq -r .postgresServerName.value)
APP_INSIGHTS_KEY=$(echo "$OUTPUTS" | jq -r .applicationInsightsInstrumentationKey.value)

echo ""
echo "=== Deployment Complete ==="
echo "Container App URL: $CONTAINER_APP_URL"
echo "Container Registry: $ACR_LOGIN_SERVER"
echo "PostgreSQL Server: $POSTGRES_SERVER"
echo "Application Insights Key: $APP_INSIGHTS_KEY"
echo ""
echo "=== GitHub Secrets to Configure ==="
echo "AZURE_CREDENTIALS_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): Create a service principal with contributor access"
echo "AZURE_RESOURCE_GROUP_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): $RESOURCE_GROUP"
echo "ACR_USERNAME: $ACR_NAME"
echo "ACR_PASSWORD: $(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)"
echo "DATABASE_CONNECTION_STRING_$(echo $ENVIRONMENT | tr '[:lower:]' '[:upper:]'): Host=$POSTGRES_SERVER.postgres.database.azure.com;Database=househeroes;Username=$POSTGRES_ADMIN_LOGIN;Password=$POSTGRES_ADMIN_PASSWORD;SSL Mode=Require;"
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