#!/bin/bash

# HouseHeroes Application Deployment Script
# Usage: ./deploy-app.sh <environment> [image-tag]

set -e

ENVIRONMENT=${1:-staging}
IMAGE_TAG=${2:-latest}
TF_BACKEND_FILE="${ENVIRONMENT}.tfbackend"

echo "Deploying HouseHeroes application for environment: $ENVIRONMENT"
echo "Image Tag: $IMAGE_TAG"

# Point Terraform at this environment's remote state
echo "Initializing Terraform..."
terraform init -reconfigure -backend-config="$TF_BACKEND_FILE" > /dev/null

# Get infrastructure details from Terraform outputs
echo "Getting infrastructure details from Terraform..."
RESOURCE_GROUP=$(terraform output -raw resource_group_name 2>/dev/null || echo "")

if [ -z "$RESOURCE_GROUP" ]; then
    echo "Error: Could not get infrastructure details from Terraform outputs."
    echo "Make sure you have deployed infrastructure first with ./deploy-terraform.sh $ENVIRONMENT"
    exit 1
fi

CONTAINER_APP=$(terraform output -raw container_app_name)
MIGRATION_JOB=$(terraform output -raw migration_job_name)
ACR_NAME=$(terraform output -raw container_registry_name)
ACR_LOGIN_SERVER=$(terraform output -raw container_registry_login_server)

echo "Resource Group: $RESOURCE_GROUP"

API_IMAGE="$ACR_LOGIN_SERVER/househeroes-api:$IMAGE_TAG"
MIGRATION_IMAGE="$ACR_LOGIN_SERVER/househeroes-migration:$IMAGE_TAG"

echo "API image: $API_IMAGE"
echo "Migration image: $MIGRATION_IMAGE"

# Login to ACR
echo "Logging into Azure Container Registry..."
az acr login --name "$ACR_NAME"

# Build and push the Docker images from the parent directory.
# Container Apps runs linux/amd64, so pin the platform (matters on Apple Silicon).
echo "Building Docker images..."
cd ..
docker build --platform linux/amd64 --target final -t "$API_IMAGE" .
docker build --platform linux/amd64 --target migration -t "$MIGRATION_IMAGE" .

echo "Pushing Docker images to ACR..."
docker push "$API_IMAGE"
docker push "$MIGRATION_IMAGE"
cd infra

# Run database migrations before the new API revision goes live
echo "Updating migration job with new image..."
az containerapp job update \
    --name "$MIGRATION_JOB" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$MIGRATION_IMAGE" \
    --output none

echo "Starting migration job..."
MIGRATION_EXECUTION_NAME=$(az containerapp job start \
    --name "$MIGRATION_JOB" \
    --resource-group "$RESOURCE_GROUP" \
    --query "name" -o tsv)
echo "Migration job execution: $MIGRATION_EXECUTION_NAME"

echo "Waiting for migration job to complete..."
while true; do
    MIGRATION_STATUS=$(az containerapp job execution show \
        --name "$MIGRATION_JOB" \
        --resource-group "$RESOURCE_GROUP" \
        --job-execution-name "$MIGRATION_EXECUTION_NAME" \
        --query "properties.status" -o tsv)

    case "$MIGRATION_STATUS" in
        Succeeded)
            echo "Migration job succeeded"
            break
            ;;
        Failed|Stopped|Degraded)
            echo "Error: Migration job finished with status: $MIGRATION_STATUS"
            echo "Check the logs with:"
            echo "az containerapp job logs show --name $MIGRATION_JOB --resource-group $RESOURCE_GROUP --execution $MIGRATION_EXECUTION_NAME --container migration"
            exit 1
            ;;
        *)
            echo "Migration status: $MIGRATION_STATUS"
            sleep 10
            ;;
    esac
done

# Update Container App with the new image
echo "Updating Container App with new image..."
az containerapp update \
    --name "$CONTAINER_APP" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$API_IMAGE" \
    --output none

echo ""
echo "=== Application Deployment Complete ==="
echo "API image deployed: $API_IMAGE"
echo "Migration image deployed: $MIGRATION_IMAGE"
echo "Container App: $CONTAINER_APP"
echo ""
echo "Check the application status:"
echo "az containerapp show --name $CONTAINER_APP --resource-group $RESOURCE_GROUP --query 'properties.latestRevisionFqdn' -o tsv"
