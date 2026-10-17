# Azure Deployment Setup for HouseHeroes

This guide walks through setting up Azure infrastructure and CI/CD for the HouseHeroes application.

Infrastructure is defined with Terraform. See the [Terraform Setup Guide](README-Terraform-Setup.md) for configuration details, variables and state management.

## Prerequisites

- Azure CLI installed and authenticated
- Terraform 1.0+ installed
- GitHub repository with Actions enabled
- Docker installed locally (for testing)

## Infrastructure Setup

### 1. Deploy Azure Resources

```bash
cd infra

# Provision staging infrastructure (creates the resource group too)
./deploy-terraform.sh staging

# Build the API and migration images, push them to ACR, run migrations, then update the API
./deploy-app.sh staging [image-tag]

# Same for production
./deploy-terraform.sh production
./deploy-app.sh production [image-tag]
```

Terraform state is local and shared between environments, so run `deploy-app.sh` for the environment you last applied with `deploy-terraform.sh`. The script refuses to run otherwise.

`deploy-terraform.sh` creates:
- Azure Container Registry (ACR)
- SQL Server
- Container App Environment
- Container App for the API
- Log Analytics Workspace
- Application Insights
- Container App Job for database migrations

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

#### Staging Environment
- `AZURE_CREDENTIALS_STAGING`: JSON output of the `az ad sp create-for-rbac` command above
- `SQL_SERVER_ADMIN_PASSWORD_STAGING`: SQL Server administrator password

#### Production Environment
- `AZURE_CREDENTIALS_PRODUCTION`: JSON output of `az ad sp create-for-rbac` for `sp-househeroes-github-production`
- `SQL_SERVER_ADMIN_PASSWORD_PRODUCTION`: SQL Server administrator password

#### Optional Secrets
- `CODECOV_TOKEN`: For code coverage reporting

## CI/CD Pipeline Features

### Build Pipeline
- Builds and tests .NET solution
- Creates multi-platform MAUI apps (Android, iOS, macOS, Windows)
- Runs security scans (CodeQL, vulnerability checks)
- Generates code coverage reports

### Deployment Pipeline
- **Staging**: Deploys from `develop` branch to staging environment
- **Production**: Deploys from `main`/`master` branch to production environment
- Uses Container Apps with auto-scaling
- Runs `infra/deploy-app.sh`, which applies database migrations before switching the API to the new image

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

`infra/deploy-app.sh` does all of this. To run a step by hand:

### Build and Push Docker Images
The `Dockerfile` has two targets: `final` (the API) and `migration` (an EF Core migrations bundle). Container Apps runs `linux/amd64`.

```bash
az acr login --name househeroesacrstaging

docker build --platform linux/amd64 --target final -t househeroesacrstaging.azurecr.io/househeroes-api:latest .
docker build --platform linux/amd64 --target migration -t househeroesacrstaging.azurecr.io/househeroes-migration:latest .
docker push househeroesacrstaging.azurecr.io/househeroes-api:latest
docker push househeroesacrstaging.azurecr.io/househeroes-migration:latest
```

### Run Database Migration
```bash
az containerapp job update \
  --name househeroes-migration-job \
  --resource-group rg-househeroes-staging \
  --image househeroesacrstaging.azurecr.io/househeroes-migration:latest

az containerapp job start \
  --name househeroes-migration-job \
  --resource-group rg-househeroes-staging
```

### Update Container App
```bash
az containerapp update \
  --name househeroes-api-staging \
  --resource-group rg-househeroes-staging \
  --image househeroesacrstaging.azurecr.io/househeroes-api:latest
```

Terraform ignores image changes on the container app and the migration job, so re-running `deploy-terraform.sh` doesn't revert them.

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

## Security Features

- Container Registry with admin user enabled for CI/CD
- SQL Server with encryption required
- Firewall rule allowing Azure services to reach the database
- Service principal scoped to the subscription (Terraform creates the resource group)
- Secrets stored as Container App secrets

## Troubleshooting

### Common Issues
1. **ACR Authentication**: Ensure the service principal in `AZURE_CREDENTIALS_*` can push to the registry
2. **Database Connection**: Check firewall rules and connection strings
3. **Container Start Issues**: Review container logs for startup errors
4. **Migration Failures**: Verify database permissions and connection

### Useful Commands
```bash
# Check container app status
az containerapp show --name househeroes-api-staging --resource-group rg-househeroes-staging

# View recent revisions
az containerapp revision list --name househeroes-api-staging --resource-group rg-househeroes-staging

# Scale manually
az containerapp update --name househeroes-api-staging --resource-group rg-househeroes-staging --min-replicas 2
```