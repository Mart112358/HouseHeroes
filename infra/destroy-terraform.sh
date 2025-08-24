#!/bin/bash

# HouseHeroes Azure Infrastructure Destruction Script (Terraform)
# Usage: ./destroy-terraform.sh <environment> [resource-group-name]

set -e

ENVIRONMENT=${1:-staging}
RESOURCE_GROUP=${2:-"rg-househeroes-$ENVIRONMENT"}
TF_VAR_FILE="${ENVIRONMENT}.tfvars"

echo "Destroying HouseHeroes infrastructure with Terraform for environment: $ENVIRONMENT"
echo "Resource Group: $RESOURCE_GROUP"
echo "Terraform vars file: $TF_VAR_FILE"

# Check if terraform is installed
if ! command -v terraform &> /dev/null; then
    echo "Error: Terraform is not installed. Please install Terraform first."
    exit 1
fi

# Check if tfvars file exists
if [ ! -f "$TF_VAR_FILE" ]; then
    echo "Error: Terraform variables file '$TF_VAR_FILE' not found."
    echo "Please create it based on terraform.tfvars.example"
    exit 1
fi

# Check if Terraform state exists
if [ ! -f "terraform.tfstate" ]; then
    echo "Warning: No Terraform state file found."
    echo "If you want to delete the resource group directly, run:"
    echo "az group delete --name '$RESOURCE_GROUP' --yes"
    exit 1
fi

# Plan the destruction
echo "Planning Terraform destroy..."
terraform plan \
    -destroy \
    -var-file="$TF_VAR_FILE" \
    -var="resource_group_name=$RESOURCE_GROUP" \
    -out=destroy.tfplan

# Prompt for confirmation unless --auto-approve is passed
if [[ "$*" != *"--auto-approve"* ]]; then
    echo ""
    echo "WARNING: This will destroy all infrastructure shown above!"
    echo "Are you sure you want to proceed? (y/N)"
    read -r response
    if [[ ! "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        echo "Destruction cancelled."
        exit 0
    fi
fi

# Apply the destroy plan
echo "Destroying infrastructure..."
terraform apply destroy.tfplan

# Clean up service principal
echo "Cleaning up service principal..."
SP_NAME="sp-househeroes-github-$ENVIRONMENT"
az ad sp delete --id $(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv) 2>/dev/null || echo "Service principal not found or already deleted"

# Clean up plan files
rm -f tfplan destroy.tfplan

echo ""
echo "=== Destruction Complete ==="
echo "All infrastructure for environment '$ENVIRONMENT' has been destroyed."
echo "Terraform state has been updated to reflect the changes."