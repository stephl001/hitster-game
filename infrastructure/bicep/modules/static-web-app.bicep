// static-web-app.bicep - Azure Static Web App for Vue.js frontend

@description('The Azure region where resources will be deployed')
param location string

@description('Environment name (e.g., dev, staging, prod)')
param environment string

@description('Tags to apply to resources')
param tags object = {}

@description('Static Web App SKU (Free or Standard)')
param sku string = 'Free'

// Static Web App
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: 'stapp-songster-web-${environment}'
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    repositoryUrl: '' // Will be configured via GitHub Actions
    branch: '' // Will be configured via GitHub Actions
    buildProperties: {
      appLocation: '/frontend'
      apiLocation: ''
      outputLocation: 'dist'
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
  }
}

// Outputs
output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
// Note: Deployment token should be retrieved separately using Azure CLI for security
// az staticwebapp secrets list --name <name> --query properties.apiKey -o tsv
