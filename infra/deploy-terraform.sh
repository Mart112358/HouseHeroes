#!/bin/bash

# HouseHeroes Azure Infrastructure Deployment Script (Pure Terraform)
# Usage: ./deploy-terraform.sh <environment>

set -e

ENVIRONMENT=${1:-staging}
RESOURCE_GROUP="rg-househeroes-$ENVIRONMENT"
TF_VAR_FILE="${ENVIRONMENT}.tfvars"

echo "Deploying HouseHeroes infrastructure with Terraform for environment: $ENVIRONMENT"
echo "Resource Group: $RESOURCE_GROUP"
echo "Terraform vars file: $TF_VAR_FILE"

# Check if terraform is installed
if ! command -v terraform &> /dev/null; then
    echo "Error: Terraform is not installed. Please install Terraform first."
    echo "Visit: https://developer.hashicorp.com/terraform/tutorials/aws-get-started/install-cli"
    exit 1
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
terraform output

echo ""
echo "=== Infrastructure Deployment Complete ==="
echo "To build, migrate and deploy the application, run:"
echo "./deploy-app.sh $ENVIRONMENT [image-tag]"
echo ""
echo "=== Terraform State ==="
echo "Terraform state is stored locally. Consider using remote state for production."
