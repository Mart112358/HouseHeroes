#!/bin/bash

# HouseHeroes Azure Infrastructure Destruction Script (Terraform)
# Usage: ./destroy-terraform.sh <environment> [--auto-approve]

set -e

ENVIRONMENT=${1:-staging}
RESOURCE_GROUP="rg-househeroes-$ENVIRONMENT"
TF_VAR_FILE="${ENVIRONMENT}.tfvars"
TF_BACKEND_FILE="${ENVIRONMENT}.tfbackend"

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

# Check if backend config exists
if [ ! -f "$TF_BACKEND_FILE" ]; then
    echo "Error: Terraform backend config '$TF_BACKEND_FILE' not found."
    exit 1
fi

# Initialize Terraform with this environment's remote state
echo "Initializing Terraform..."
terraform init -reconfigure -backend-config="$TF_BACKEND_FILE"

# Check if Terraform state has anything to destroy
if [ -z "$(terraform state list)" ]; then
    echo "Warning: Terraform state for '$ENVIRONMENT' is empty."
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

# Clean up plan files
rm -f tfplan destroy.tfplan

echo ""
echo "=== Destruction Complete ==="
echo "All infrastructure for environment '$ENVIRONMENT' has been destroyed."
echo "Terraform state has been updated to reflect the changes."
