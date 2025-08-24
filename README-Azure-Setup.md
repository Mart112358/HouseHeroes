# Azure Deployment Setup for HouseHeroes

This guide walks through setting up Azure infrastructure and CI/CD for the HouseHeroes application.

## Infrastructure-as-Code Options

Choose between two infrastructure deployment methods:

- **🔥 Terraform** (Recommended): Modern, multi-cloud IaC tool with better state management
  - See [Terraform Setup Guide](README-Terraform-Setup.md) for detailed instructions
  - Better for teams, version control, and complex deployments
  
- **🔧 Bicep** (Legacy): Azure-native ARM template language
  - Instructions below for existing Bicep setup
  - Simpler for Azure-only deployments

## Prerequisites

- Azure CLI installed and authenticated
- GitHub repository with Actions enabled
- Docker installed locally (for testing)

## Infrastructure Setup

### 1. Deploy Azure Resources

```bash
# Deploy staging environment
cd infra
./deploy.sh staging

# Deploy production environment  
./deploy.sh production
```

The script will create:
- Azure Container Registry (ACR)
- SQL Server
- Container App Environment
- Container App for the API
- Log Analytics Workspace
- Application Insights
- Container App Job for database migrations

### 2. Configure GitHub Secrets

After running the deployment script, configure these secrets in your GitHub repository:

#### Staging Environment
- `AZURE_CREDENTIALS_STAGING`: Service principal JSON from deployment output
- `AZURE_RESOURCE_GROUP_STAGING`: Resource group name (e.g., `rg-househeroes-staging`)
- `ACR_USERNAME`: Container registry username
- `ACR_PASSWORD`: Container registry password
- `DATABASE_CONNECTION_STRING_STAGING`: SQL Server connection string

#### Production Environment
- `AZURE_CREDENTIALS_PRODUCTION`: Service principal JSON from deployment output
- `AZURE_RESOURCE_GROUP_PRODUCTION`: Resource group name (e.g., `rg-househeroes-production`)
- `DATABASE_CONNECTION_STRING_PRODUCTION`: SQL Server connection string

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
- Runs database migrations automatically
- Blue-green deployment support

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

### Build and Push Docker Image
```bash
# Login to ACR
az acr login --name househeroesacrstaging

# Build and push
docker build -t househeroesacrstaging.azurecr.io/househeroes-api:latest .
docker push househeroesacrstaging.azurecr.io/househeroes-api:latest
```

### Update Container App
```bash
az containerapp update \
  --name househeroes-api-staging \
  --resource-group rg-househeroes-staging \
  --image househeroesacrstaging.azurecr.io/househeroes-api:latest
```

### Run Database Migration
```bash
az containerapp job start \
  --name househeroes-migration-job \
  --resource-group rg-househeroes-staging
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
az containerapp job logs show --name househeroes-migration-job --resource-group rg-househeroes-staging
```

## Cost Optimization

The infrastructure is configured for cost efficiency:
- **Staging**: Single replica, basic SQL Server tier
- **Production**: Auto-scaling 2-10 replicas, higher performance tiers
- Container Apps scale to zero when not in use
- Basic tier Container Registry for development

## Security Features

- Container Registry with admin user enabled for CI/CD
- SQL Server with encryption required
- Firewall rules restricting database access
- Service principal with least privilege access
- Secrets stored in Azure Key Vault integration (Container Apps)

## Troubleshooting

### Common Issues
1. **ACR Authentication**: Ensure ACR credentials are correct in GitHub secrets
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