# Terraform Azure Deployment Setup for HouseHeroes

This guide walks through setting up Azure infrastructure and CI/CD for the HouseHeroes application using Terraform.

## Prerequisites

- [Terraform](https://developer.hashicorp.com/terraform/tutorials/aws-get-started/install-cli) installed (version 1.0+)
- Azure CLI installed and authenticated
- GitHub repository with Actions enabled
- Docker installed locally (for testing)

## Infrastructure Setup

### 1. Deploy Azure Resources with Terraform

#### Manual Deployment

```bash
cd infra

# Copy example variables file and configure
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Initialize Terraform
terraform init

# Plan the deployment
terraform plan

# Deploy staging environment
./deploy-terraform.sh staging

# Deploy production environment  
./deploy-terraform.sh production

# Build, migrate and deploy the application (after deploy-terraform.sh)
./deploy-app.sh staging [image-tag]
```

#### Using Terraform directly

```bash
cd infra

# Initialize Terraform
terraform init

# Deploy staging
terraform apply -var-file="staging.tfvars" -var="resource_group_name=rg-househeroes-staging"

# Deploy production
terraform apply -var-file="production.tfvars" -var="resource_group_name=rg-househeroes-production"
```

The Terraform configuration creates:
- Resource group
- Azure Container Registry (ACR)
- SQL Server with database
- Container App Environment
- Container App for the API
- Container App Job for database migrations
- Log Analytics Workspace
- Application Insights

### 2. Configure GitHub Secrets

Create the GitHub Actions service principal once per environment. It needs subscription scope because Terraform creates the resource group:

```bash
az ad sp create-for-rbac \
  --name "sp-househeroes-github-staging" \
  --role contributor \
  --scopes "/subscriptions/$(az account show --query id -o tsv)" \
  --sdk-auth
```

Then configure these secrets in your GitHub repository:

#### Required Secrets (both environments)
- `AZURE_CREDENTIALS_STAGING`: JSON output of `az ad sp create-for-rbac` for staging
- `AZURE_CREDENTIALS_PRODUCTION`: JSON output of `az ad sp create-for-rbac` for production
- `SQL_SERVER_ADMIN_PASSWORD_STAGING`: SQL Server password for staging
- `SQL_SERVER_ADMIN_PASSWORD_PRODUCTION`: SQL Server password for production

#### Optional Secrets
- `CODECOV_TOKEN`: For code coverage reporting

## Terraform Configuration

### Files Structure

```
infra/
├── main.tf                    # Main Terraform configuration
├── variables.tf              # Input variables
├── outputs.tf                # Output values
├── staging.tfvars           # Staging environment variables
├── production.tfvars        # Production environment variables
├── terraform.tfvars.example # Example variables file
├── deploy-terraform.sh      # Infrastructure deployment script
├── deploy-app.sh            # Image build, migration and app deployment script
└── destroy-terraform.sh     # Destruction script
```

### Key Features

- **Secure Secrets**: SQL Server passwords can be provided or auto-generated
- **Environment-specific**: Separate .tfvars files for staging/production
- **Resource Tagging**: Consistent tagging across all resources
- **State Management**: Local state (consider remote state for production)

### Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `environment` | Environment name (staging/production) | `"staging"` | No |
| `location` | Azure region | `"Canada Central"` | No |
| `app_name` | Application name | `"househeroes"` | No |
| `resource_group_name` | Resource group name | - | Yes |
| `sql_server_admin_login` | SQL Server admin username | - | Yes |
| `sql_server_admin_password` | SQL Server admin password | `null` (auto-generated) | No |
| `tags` | Resource tags | `{ Project = "HouseHeroes", ManagedBy = "Terraform" }` | No |

## CI/CD Pipeline Features

### Terraform Integration

- **Plan on PR**: Runs `terraform plan` on pull requests for validation
- **Apply on Push**: Runs `terraform apply` on pushes to main/develop branches
- **Automated Deployment**: Runs `deploy-app.sh` after infrastructure, which reads names from Terraform outputs

### Build Pipeline
- Builds and tests .NET solution
- Creates multi-platform MAUI apps (Android, iOS, macOS, Windows)
- Runs security scans (CodeQL, vulnerability checks)
- Validates Terraform configuration

### Deployment Pipeline
- **Staging**: Deploys from `develop` branch to staging environment
- **Production**: Deploys from `main`/`master` branch to production environment
- Uses Container Apps with auto-scaling
- Applies database migrations before switching the API to the new image
- Infrastructure-as-Code with Terraform

### Mobile App Builds
- Android builds on all pushes/PRs
- iOS/macOS builds only on main branch pushes (due to runner costs)
- Windows builds only on main branch pushes
- Artifacts uploaded for distribution

## Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   MAUI Mobile   │────│   Container App  │────│   SQL Server    │
│      Apps       │    │   (API Service)  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────────┐
                       │ Application      │
                       │ Insights         │
                       └──────────────────┘
```

## Manual Deployment Commands

### Terraform Commands

```bash
# Initialize Terraform
terraform init

# Validate configuration
terraform validate

# Plan deployment
terraform plan -var-file="staging.tfvars"

# Apply changes
terraform apply -var-file="staging.tfvars"

# Show outputs
terraform output

# Destroy infrastructure
terraform destroy -var-file="staging.tfvars"
```

### Deploy the Application
```bash
./deploy-app.sh staging [image-tag]
```

This builds the `final` (API) and `migration` (EF Core migrations bundle) targets of the `Dockerfile` for `linux/amd64`, pushes both to ACR, runs the migration job and waits for it, then updates the Container App. Resource names come from Terraform outputs.

Terraform only sets placeholder images on the container app and migration job and ignores image changes afterwards, so `terraform apply` never reverts a deployed image. See [README-Azure-Setup.md](README-Azure-Setup.md#manual-deployment-commands) for the individual commands.

## State Management

### Local State (Current)
- Terraform state is stored locally in `terraform.tfstate`, shared by staging and production
- Suitable for development and small teams
- CI starts from empty state on every run
- **Risk**: State file loss means losing track of infrastructure

### Remote State (Recommended for Production)
Consider using Azure Storage for remote state:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstatexxxxx"
    container_name       = "tfstate"
    key                  = "househeroes.terraform.tfstate"
  }
}
```

## Monitoring and Logs

- **Application Insights**: Performance monitoring, error tracking
- **Log Analytics**: Container logs and metrics
- **Container App Metrics**: CPU, memory, request metrics

Access logs via:
```bash
# View container app logs
az containerapp logs show --name househeroes-api-staging --resource-group rg-househeroes-staging --follow

# View job logs
az containerapp job logs show --name househeroes-migration-job --resource-group rg-househeroes-staging --container migration
```

## Cost Optimization

The infrastructure is configured for cost efficiency:
- **Staging**: Single replica, basic SQL Server tier
- **Production**: Auto-scaling 2-10 replicas, higher performance tiers
- Container Apps keep at least one replica running (`min_replicas` is 1 in staging, 2 in production)
- Basic tier Container Registry for development
- Resource tagging for cost tracking

## Security Features

- Container Registry with admin user enabled for CI/CD
- SQL Server with encryption required
- Firewall rule allowing Azure services to reach the database
- Service principal scoped to the subscription (Terraform creates the resource group)
- Sensitive values marked as sensitive in Terraform
- Secrets stored securely in Container Apps

## Disaster Recovery

### Backup Strategy
- **Database**: Automatic SQL Server backups (7-day retention)
- **Container Images**: Stored in ACR with geo-replication available
- **Terraform State**: Should be backed up regularly

### Recovery Process
```bash
# Recreate infrastructure from Terraform
terraform apply -var-file="production.tfvars"

# Restore database from backup (if needed)
# This would be done through Azure Portal or CLI

# Redeploy application
./deploy-app.sh production
```

## Troubleshooting

### Common Issues
1. **Terraform State Lock**: Use `terraform force-unlock <lock-id>` if needed
2. **Resource Already Exists**: Import existing resources with `terraform import`
3. **ACR Authentication**: Ensure the service principal can push to the registry (`az acr login`)
4. **Database Connection**: Check firewall rules and connection strings
5. **Container Start Issues**: Review container logs for startup errors

### Useful Commands
```bash
# Check Terraform state
terraform show

# List Terraform resources
terraform state list

# Import existing resource
terraform import azurerm_resource_group.main /subscriptions/.../resourceGroups/rg-name

# Refresh state from Azure
terraform refresh

# View container app status
az containerapp show --name househeroes-api-staging --resource-group rg-househeroes-staging

# View recent revisions
az containerapp revision list --name househeroes-api-staging --resource-group rg-househeroes-staging
```

## Adopting Existing Resources

The resource group is managed by Terraform. If resources already exist in Azure but not in Terraform state, import them before applying:

```bash
# Import resource group
terraform import azurerm_resource_group.main /subscriptions/xxx/resourceGroups/rg-househeroes-staging

# Import other resources similarly
terraform import azurerm_container_registry.main /subscriptions/xxx/resourceGroups/rg-househeroes-staging/providers/Microsoft.ContainerRegistry/registries/househeroesacrstaging
```