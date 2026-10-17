#!/bin/bash

# HouseHeroes Terraform Remote State Bootstrap Script
# Creates the storage account that holds Terraform state for all environments.
# Run once per subscription, before the first ./deploy-terraform.sh.
# Usage: ./bootstrap-state.sh

set -e

# Read the backend settings from staging.tfbackend so the names live in one place
tfbackend_value() {
    sed -n "s/^$1 *= *\"\(.*\)\"/\1/p" staging.tfbackend
}

RESOURCE_GROUP=$(tfbackend_value resource_group_name)
STORAGE_ACCOUNT=$(tfbackend_value storage_account_name)
CONTAINER=$(tfbackend_value container_name)
LOCATION="Canada Central"

echo "Bootstrapping Terraform remote state"
echo "Resource Group: $RESOURCE_GROUP"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "Container: $CONTAINER"

# Storage account names are global; registering the provider is required before checking
echo "Registering Microsoft.Storage resource provider..."
az provider register --namespace Microsoft.Storage --wait

if ! az storage account show --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
    if [ "$(az storage account check-name --name "$STORAGE_ACCOUNT" --query nameAvailable -o tsv)" != "true" ]; then
        echo "Error: Storage account name '$STORAGE_ACCOUNT' is taken."
        echo "Change storage_account_name in staging.tfbackend and production.tfbackend, then re-run."
        exit 1
    fi
fi

echo "Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags Project=HouseHeroes ManagedBy=bootstrap-state.sh \
    --output none

echo "Creating storage account..."
az storage account create \
    --name "$STORAGE_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --sku Standard_LRS \
    --kind StorageV2 \
    --min-tls-version TLS1_2 \
    --allow-blob-public-access false \
    --tags Project=HouseHeroes ManagedBy=bootstrap-state.sh \
    --output none

# Versioning lets you recover a previous state file if one gets corrupted
echo "Enabling blob versioning..."
az storage account blob-service-properties update \
    --account-name "$STORAGE_ACCOUNT" \
    --resource-group "$RESOURCE_GROUP" \
    --enable-versioning true \
    --output none

echo "Creating state container..."
az storage container create \
    --name "$CONTAINER" \
    --account-name "$STORAGE_ACCOUNT" \
    --auth-mode key \
    --output none

echo ""
echo "=== Remote State Ready ==="
echo "Deploy infrastructure with: ./deploy-terraform.sh staging"
