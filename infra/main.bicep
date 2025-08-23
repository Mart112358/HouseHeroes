@description('The environment name (staging, production)')
param environment string = 'staging'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The application name')
param appName string = 'househeroes'

@description('The PostgreSQL administrator login')
@secure()
param postgresAdminLogin string

@description('The PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('The container registry name')
param containerRegistryName string = '${appName}acr${environment}'

var containerAppEnvironmentName = '${appName}-env-${environment}'
var containerAppName = '${appName}-api-${environment}'
var postgresServerName = '${appName}-db-${environment}'
var logAnalyticsWorkspaceName = '${appName}-logs-${environment}'
var applicationInsightsName = '${appName}-insights-${environment}'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 90
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// PostgreSQL Database
resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name: 'househeroes'
  properties: {
    charset: 'utf8'
    collation: 'en_US.utf8'
  }
}

// PostgreSQL Firewall Rule to allow Azure services
resource postgresFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Container App Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.name
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        {
          name: 'connection-string'
          value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=househeroes;Username=${postgresAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require;'
        }
        {
          name: 'app-insights-connection-string'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'househeroes-api'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'  // Placeholder image for initial deployment
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'production' ? 'Production' : 'Staging'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: environment == 'production' ? 2 : 1
        maxReplicas: environment == 'production' ? 10 : 3
        rules: [
          {
            name: 'http-scaling-rule'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

// Migration Job Container App (will be created after initial image is pushed)
resource migrationJob 'Microsoft.App/jobs@2023-05-01' = {
  name: '${appName}-migration-job'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      replicaTimeout: 1800
      replicaRetryLimit: 3
      triggerType: 'Manual'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.name
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        {
          name: 'connection-string'
          value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=househeroes;Username=${postgresAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require;'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migration'
          image: 'mcr.microsoft.com/dotnet/runtime:9.0'  // Placeholder image for initial deployment
          command: ['sleep', '30']  // Placeholder command
          env: [
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'connection-string'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

// Outputs
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output postgresServerName string = postgresServer.name
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output resourceGroupName string = resourceGroup().name