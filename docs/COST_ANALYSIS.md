# HouseHeroes Infrastructure Cost Analysis

## Overview
This document provides a detailed cost analysis for the HouseHeroes staging infrastructure deployed in Azure Canada Central region.

## Deployed Infrastructure

### Core Services
- **Resource Group**: `rg-househeroes-staging`
- **Container Registry**: `househeroesacrstaging.azurecr.io`
- **SQL Server**: `househeroes-db-staging.database.windows.net`
- **SQL Database**: `househeroes` (Basic, 5 DTU, 2GB)
- **Container App**: `househeroes-api-staging`
- **Container App Environment**: `househeroes-env-staging`
- **Migration Job**: `househeroes-migration-job`
- **Log Analytics Workspace**: `househeroes-logs-staging`
- **Application Insights**: `househeroes-insights-staging`

### Application URL
Run `terraform output container_app_url` in `infra/` (the hostname changes with each revision).

## Monthly Cost Estimate (CAD) - Canada Central Region

### Service Breakdown

| Service | Configuration | USD/Month | CAD/Month | Notes |
|---------|---------------|-----------|-----------|-------|
| **Azure SQL Database** | Basic (5 DTU, 2GB) | $4.90 | $6.75 | Fixed monthly cost |
| **Container Registry** | Basic (10GB included) | $5.01 | $6.90 | $0.167/day |
| **Container Apps** | Consumption-based | $0-11 | $0-15 | Free tier: 180K vCPU-s, 360K GiB-s, 2M requests |
| **Log Analytics** | Pay-as-you-go | $0-7 | $0-10 | First 5GB/month free, then $2.76/GB |
| **Application Insights** | Pay-as-you-go | $0-7 | $0-10 | First 5GB/month free, then $2.76/GB |
| **Storage & Networking** | Various | $1-4 | $1-5 | Data transfer, additional storage |

### Total Cost Estimates

| Usage Level | Description | Monthly Cost (CAD) |
|-------------|-------------|-------------------|
| **Light Usage** | Development/Testing with minimal traffic | $15-20 |
| **Moderate Usage** | Active staging environment | $25-35 |
| **Heavy Usage** | Production-like load testing | $35-45 |

### Recommended Budget Range: **$20-35 CAD/month**

## Cost Optimization Strategies

### Immediate Actions
1. **Monitor Free Tier Usage**
   - Container Apps: 180,000 vCPU-seconds free/month
   - Log Analytics: First 5GB ingestion free/month
   - Application Insights: First 5GB ingestion free/month

2. **Set Daily Caps**
   - Log Analytics: 160 MB/day to stay under 5GB/month
   - Application Insights: 160 MB/day to stay under 5GB/month

3. **Container Apps Optimization**
   - Configure scale-to-zero for non-production hours
   - Use minimal resource allocation (0.25 vCPU, 0.5 GiB)

### Long-term Optimizations
4. **Resource Monitoring**
   - Set up cost alerts at $25 CAD threshold
   - Regular review of resource usage patterns
   - Clean up unused container images in ACR

5. **Environment Management**
   - Consider automated start/stop for non-business hours
   - Use Azure DevTest pricing where applicable

## Cost Breakdown by Component

### Fixed Costs (Always Running)
- **SQL Database Basic**: $6.75 CAD/month
- **Container Registry Basic**: $6.90 CAD/month
- **Total Fixed**: $13.65 CAD/month

### Variable Costs (Usage-Based)
- **Container Apps Compute**: $0-15 CAD/month
- **Log Analytics Ingestion**: $0-10 CAD/month  
- **Application Insights**: $0-10 CAD/month
- **Networking**: $1-5 CAD/month

## Monitoring and Alerts

### Recommended Cost Alerts
1. **Daily Alert**: >$1.50 CAD/day
2. **Monthly Alert**: >$30 CAD/month
3. **Spending Threshold**: >$45 CAD/month (emergency alert)

### Key Metrics to Monitor
- Container Apps vCPU-seconds and GiB-seconds consumption
- Log Analytics data ingestion volume (GB/day)
- Application Insights telemetry volume (GB/day)
- SQL Database DTU utilization

## Regional Pricing Notes

- **Currency**: Prices converted from USD using approximate rate (1 USD = 1.38 CAD)
- **Region**: Canada Central pricing used
- **Date**: Analysis performed August 2025
- **Disclaimer**: Actual costs may vary based on usage patterns, current exchange rates, and Azure pricing changes

## Cost Management Commands

```bash
# View current month costs
az consumption usage list --top 10

# Set up budget alert
az consumption budget create \
  --amount 35 \
  --budget-name "HouseHeroes-Staging" \
  --category "Cost" \
  --time-grain "Monthly" \
  --time-period start-date="2025-01-01" end-date="2026-01-01"

# Monitor resource costs
az cost-management query \
  --scope "/subscriptions/{subscription-id}/resourceGroups/rg-househeroes-staging" \
  --timeframe "MonthToDate"
```

## Next Steps

1. **Deploy cost monitoring dashboard**
2. **Set up automated cost alerts**
3. **Implement resource tagging for better cost tracking**
4. **Review costs monthly and optimize accordingly**
5. **Consider Azure Cost Management recommendations**

---

**Last Updated**: August 2025  
**Review Date**: Monthly  
**Contact**: Infrastructure Team