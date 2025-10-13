// main.bicep - Main orchestrator for Songster Game Azure infrastructure

targetScope = 'resourceGroup'

@description('The Azure region where resources will be deployed')
param location string = resourceGroup().location

@description('Environment name (e.g., dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string

@description('App Service Plan SKU')
@allowed([
  'F1'  // Free
  'B1'  // Basic
  'S1'  // Standard
])
param appServicePlanSku string = 'F1'

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Free'

@description('Frontend URL for CORS configuration')
param frontendUrl string

@description('Spotify Redirect URI')
param spotifyRedirectUri string

// Common tags for all resources
var commonTags = {
  Environment: environment
  Project: 'Songster'
  ManagedBy: 'Bicep'
  CostCenter: 'Development'
}

// Deploy monitoring resources (Application Insights + Log Analytics)
module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  params: {
    location: location
    environment: environment
    tags: commonTags
  }
}

// Deploy App Service (Backend API) - Initial deployment without Key Vault URI
module appService './modules/app-service.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    environment: environment
    tags: commonTags
    appServicePlanSku: appServicePlanSku
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    keyVaultUri: '' // Will be updated after Key Vault is created
    frontendUrl: frontendUrl
    spotifyRedirectUri: spotifyRedirectUri
  }
}

// Deploy Key Vault (depends on App Service for managed identity)
module keyVault './modules/key-vault.bicep' = {
  name: 'key-vault-deployment'
  params: {
    location: location
    environment: environment
    tags: commonTags
    appServicePrincipalId: appService.outputs.appServicePrincipalId
  }
}

// Update App Service with Key Vault URI
module appServiceUpdate './modules/app-service.bicep' = {
  name: 'app-service-update-deployment'
  params: {
    location: location
    environment: environment
    tags: commonTags
    appServicePlanSku: appServicePlanSku
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    keyVaultUri: keyVault.outputs.keyVaultUri
    frontendUrl: frontendUrl
    spotifyRedirectUri: spotifyRedirectUri
  }
}

// Deploy Static Web App (Frontend)
module staticWebApp './modules/static-web-app.bicep' = {
  name: 'static-web-app-deployment'
  params: {
    location: location
    environment: environment
    tags: commonTags
    sku: staticWebAppSku
  }
}

// Outputs
output resourceGroupName string = resourceGroup().name
output environment string = environment

// Monitoring outputs
output applicationInsightsName string = monitoring.outputs.applicationInsightsId
output applicationInsightsInstrumentationKey string = monitoring.outputs.applicationInsightsInstrumentationKey
output applicationInsightsConnectionString string = monitoring.outputs.applicationInsightsConnectionString

// Backend outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output appServicePrincipalId string = appService.outputs.appServicePrincipalId

// Key Vault outputs
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri

// Frontend outputs
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
