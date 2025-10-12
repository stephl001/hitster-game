// app-service.bicep - App Service Plan and App Service for ASP.NET Core API

@description('The Azure region where resources will be deployed')
param location string

@description('Environment name (e.g., dev, staging, prod)')
param environment string

@description('Tags to apply to resources')
param tags object = {}

@description('App Service Plan SKU (e.g., F1, B1, S1)')
param appServicePlanSku string = 'F1'

@description('Application Insights Connection String')
param applicationInsightsConnectionString string

@description('Key Vault URI for secret references (optional, can be empty for initial deployment)')
param keyVaultUri string = ''

@description('Frontend URL for CORS configuration')
param frontendUrl string

@description('Spotify Redirect URI')
param spotifyRedirectUri string

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'asp-songster-${environment}'
  location: location
  tags: tags
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'F1' ? 'Free' : 'Basic'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'app-songster-api-${environment}'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: appServicePlanSku != 'F1' // Cannot be enabled on F1
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      webSocketsEnabled: true // Required for SignalR
      cors: {
        allowedOrigins: [
          frontendUrl
          'http://localhost:5173' // Vite dev server
          'http://localhost:4173' // Vite preview
        ]
        supportCredentials: true
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'dev' ? 'Development' : 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'Spotify__ClientId'
          value: keyVaultUri != '' ? '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/SpotifyClientId/)' : 'PLACEHOLDER'
        }
        {
          name: 'Spotify__ClientSecret'
          value: keyVaultUri != '' ? '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/SpotifyClientSecret/)' : 'PLACEHOLDER'
        }
        {
          name: 'Spotify__RedirectUri'
          value: spotifyRedirectUri
        }
        {
          name: 'Frontend__Url'
          value: frontendUrl
        }
      ]
    }
  }
}

// Outputs
output appServiceId string = appService.id
output appServiceName string = appService.name
output appServicePrincipalId string = appService.identity.principalId
output appServiceDefaultHostname string = appService.properties.defaultHostName
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
