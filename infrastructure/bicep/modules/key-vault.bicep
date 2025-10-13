// key-vault.bicep - Azure Key Vault for storing secrets

@description('The Azure region where resources will be deployed')
param location string

@description('Environment name (e.g., dev, staging, prod)')
param environment string

@description('Tags to apply to resources')
param tags object = {}

@description('Tenant ID for Key Vault access policies')
param tenantId string = subscription().tenantId

// Generate a unique suffix for Key Vault name (must be globally unique and max 24 chars)
// uniqueString returns 13 chars, so we take first 8 chars to ensure name fits in 24 char limit
var uniqueSuffix = substring(uniqueString(resourceGroup().id), 0, 8)

// Key Vault (name must be 3-24 alphanumeric chars, begin with letter, end with letter/digit, no consecutive hyphens)
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'kv-song-${environment}-${uniqueSuffix}'
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
    accessPolicies: [] // Access policies will be added via separate module
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
