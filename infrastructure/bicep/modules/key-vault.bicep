// key-vault.bicep - Azure Key Vault for storing secrets

@description('The Azure region where resources will be deployed')
param location string

@description('Environment name (e.g., dev, staging, prod)')
param environment string

@description('Tags to apply to resources')
param tags object = {}

@description('Principal ID of the App Service to grant access to Key Vault')
param appServicePrincipalId string

@description('Tenant ID for Key Vault access policies')
param tenantId string = subscription().tenantId

// Generate a unique suffix for Key Vault name (must be globally unique)
var uniqueSuffix = uniqueString(resourceGroup().id)

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'kv-songster-${environment}-${uniqueSuffix}'
  location: location
  tags: tags
  properties: {
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    tenantId: tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: appServicePrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
