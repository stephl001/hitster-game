// key-vault-access.bicep - Grant App Service access to Key Vault

@description('Name of the Key Vault')
param keyVaultName string

@description('Principal ID of the App Service to grant access')
param appServicePrincipalId string

@description('Tenant ID for access policies')
param tenantId string = subscription().tenantId

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

// Add access policy for App Service
resource accessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  name: 'add'
  parent: keyVault
  properties: {
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
  }
}
