# Azure Infrastructure Deployment

This directory contains Azure infrastructure as code (IaC) using Bicep templates for deploying the Songster Game application.

## Architecture Overview

The infrastructure consists of the following Azure resources:

- **Resource Group**: Container for all resources
- **App Service Plan + App Service**: Hosts the ASP.NET Core 9 backend API
- **Static Web App**: Hosts the Vue.js 3 frontend
- **Key Vault**: Stores secrets (Spotify credentials)
- **Application Insights**: Application monitoring and logging
- **Log Analytics Workspace**: Centralized logging

## Directory Structure

```
infrastructure/
├── bicep/
│   ├── main.bicep                    # Main orchestrator template
│   ├── modules/                      # Reusable Bicep modules
│   │   ├── app-service.bicep        # Backend App Service resources
│   │   ├── static-web-app.bicep     # Frontend Static Web App
│   │   ├── key-vault.bicep          # Key Vault for secrets
│   │   └── monitoring.bicep         # Application Insights & Log Analytics
│   └── parameters/
│       └── dev.parameters.json      # Dev environment parameters
├── scripts/
│   ├── deploy-dev.sh                # Bash deployment script
│   └── deploy-dev.ps1               # PowerShell deployment script
└── README.md                        # This file
```

## Prerequisites

1. **Azure CLI** installed and configured
   - Install: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
   - Login: `az login`

2. **Azure Subscription** with appropriate permissions
   - Contributor role on the subscription or resource group

3. **Spotify Developer Account** (for API credentials)
   - Get your credentials at: https://developer.spotify.com/dashboard

4. **Git** (for version control)

## Deployment Instructions

### Option 1: Using Deployment Scripts (Recommended)

#### Windows (PowerShell)
```powershell
cd infrastructure\scripts
.\deploy-dev.ps1
```

#### Linux/Mac (Bash)
```bash
cd infrastructure/scripts
chmod +x deploy-dev.sh
./deploy-dev.sh
```

The script will:
1. Verify Azure CLI authentication
2. Create the resource group
3. Validate the Bicep template
4. Deploy all infrastructure resources
5. Display deployment outputs
6. Save outputs to `.azure/deployment-outputs-dev.json`

### Option 2: Manual Deployment with Azure CLI

```bash
# 1. Login to Azure
az login

# 2. Set your subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# 3. Create resource group
az group create \
  --name rg-songster-dev \
  --location eastus

# 4. Deploy Bicep template
az deployment group create \
  --resource-group rg-songster-dev \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters/dev.parameters.json
```

### Option 3: GitHub Actions (CI/CD)

See [CI/CD Setup](#cicd-setup-with-github-actions) section below.

## Post-Deployment Configuration

After infrastructure deployment, complete these steps:

### 1. Add Spotify Secrets to Key Vault

```bash
# Get your Key Vault name from deployment outputs
KEY_VAULT_NAME=$(az deployment group show \
  --resource-group rg-songster-dev \
  --name songster-dev-TIMESTAMP \
  --query properties.outputs.keyVaultName.value -o tsv)

# Add Spotify credentials
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name SpotifyClientId \
  --value "YOUR_SPOTIFY_CLIENT_ID"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name SpotifyClientSecret \
  --value "YOUR_SPOTIFY_CLIENT_SECRET"
```

### 2. Deploy Backend Application

```bash
cd ../backend/SongsterGame.Api

# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../publish.zip .
cd ..

# Deploy to App Service
az webapp deploy \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev \
  --src-path ./publish.zip \
  --type zip
```

### 3. Deploy Frontend Application

```bash
cd ../frontend

# Install dependencies
npm ci

# Build with environment variables
VITE_API_URL=https://app-songster-api-dev.azurewebsites.net \
VITE_SPOTIFY_CLIENT_ID=YOUR_SPOTIFY_CLIENT_ID \
npm run build

# Deploy using Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli
swa deploy ./dist \
  --deployment-token $(az staticwebapp secrets list \
    --name stapp-songster-web-dev \
    --resource-group rg-songster-dev \
    --query properties.apiKey -o tsv)
```

## Environment Configuration

### Dev Environment (Free Tier)

The dev environment uses **free tier resources** to minimize costs:

| Resource | SKU | Cost | Limitations |
|----------|-----|------|-------------|
| App Service Plan | F1 (Free) | $0 | 60 CPU min/day, 1GB RAM, cold starts |
| Static Web App | Free | $0 | No custom domains |
| Application Insights | Free | $0 | 5GB/month ingestion |
| Key Vault | Standard | ~$0 | Pay per operation (minimal cost) |
| **Total** | | **~$0/month** | Perfect for dev/testing |

**Important limitations:**
- F1 App Service has **60 CPU minutes per day** (resets at midnight UTC)
- No "Always On" setting (cold starts on first request)
- Not suitable for production workloads

### Resource Naming Convention

Resources follow Azure naming best practices:

- Resource Group: `rg-songster-{environment}`
- App Service Plan: `asp-songster-{environment}`
- App Service: `app-songster-api-{environment}`
- Static Web App: `stapp-songster-web-{environment}`
- Key Vault: `kv-songster-{environment}-{unique-id}`
- Application Insights: `appi-songster-{environment}`
- Log Analytics: `log-songster-{environment}`

## Environment Variables

### Backend (App Service)

These are configured automatically via Bicep:

```bash
ASPNETCORE_ENVIRONMENT=Development
APPLICATIONINSIGHTS_CONNECTION_STRING=[auto-configured]
Spotify__ClientId=[from Key Vault]
Spotify__ClientSecret=[from Key Vault]
Spotify__RedirectUri=https://app-songster-api-dev.azurewebsites.net/api/auth/callback
Frontend__Url=https://stapp-songster-web-dev.azurestaticapps.net
```

### Frontend (Static Web App)

Set these during build:

```bash
VITE_API_URL=https://app-songster-api-dev.azurewebsites.net
VITE_SPOTIFY_CLIENT_ID=your_spotify_client_id
```

## CI/CD Setup with GitHub Actions

### Required GitHub Secrets

Configure these secrets in your GitHub repository (Settings → Secrets and variables → Actions):

#### For Azure Authentication (Federated Identity - Recommended)
```
AZURE_CLIENT_ID=<service-principal-client-id>
AZURE_TENANT_ID=<azure-tenant-id>
AZURE_SUBSCRIPTION_ID=<subscription-id>
```

#### For Static Web App Deployment
```
AZURE_STATIC_WEB_APP_API_TOKEN=<token-from-deployment-output>
SPOTIFY_CLIENT_ID=<your-spotify-client-id>
```

### Setting up Azure Service Principal with Federated Identity

```bash
# Create service principal
az ad sp create-for-rbac \
  --name sp-songster-github \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-songster-dev

# Configure federated credentials for GitHub Actions
az ad app federated-credential create \
  --id <app-id> \
  --parameters '{
    "name": "github-actions-songster",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:YOUR_GITHUB_USERNAME/hitster-game:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### Available Workflows

1. **azure-infrastructure.yml**: Deploy infrastructure (manual trigger)
2. **backend-deploy.yml**: Deploy backend on push to main/dev
3. **frontend-deploy.yml**: Deploy frontend on push to main/dev

## Monitoring and Troubleshooting

### View Application Logs

```bash
# Stream backend logs
az webapp log tail \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev

# View in Application Insights
az monitor app-insights query \
  --app appi-songster-dev \
  --resource-group rg-songster-dev \
  --analytics-query "traces | take 50"
```

### Access Key Vault Secrets

```bash
# List secrets
az keyvault secret list \
  --vault-name kv-songster-dev-XXXXX

# Get secret value
az keyvault secret show \
  --vault-name kv-songster-dev-XXXXX \
  --name SpotifyClientId \
  --query value -o tsv
```

### Restart App Service

```bash
az webapp restart \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

## Cost Management

### Dev Environment Estimated Costs

- **Free Tier (F1)**: ~$0/month
- All resources use free tiers suitable for development

### Monitor Costs

```bash
# View current costs
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --query "[?contains(instanceName, 'songster')]"
```

## Cleanup

To delete all resources:

```bash
# Delete entire resource group
az group delete \
  --name rg-songster-dev \
  --yes \
  --no-wait
```

## Upgrading to Production

When ready for production, modify parameters:

```json
{
  "appServicePlanSku": { "value": "B1" },  // Basic tier
  "staticWebAppSku": { "value": "Standard" }
}
```

Estimated production costs: ~$13-18/month (B1 tier)

## Troubleshooting Common Issues

### Issue: Deployment fails with "KeyVault name already exists"

**Solution**: Key Vault names must be globally unique. The template uses `uniqueString()` to generate a suffix, but if deploying/redeploying quickly, you may hit soft-delete retention. Either:
- Wait 7 days for soft-delete to expire
- Purge the deleted Key Vault: `az keyvault purge --name kv-songster-dev-XXXXX`

### Issue: App Service shows "503 Service Unavailable"

**Solution**: F1 tier has cold starts. Wait 30-60 seconds for the app to start, or upgrade to B1 with "Always On" enabled.

### Issue: CORS errors in frontend

**Solution**: Verify the `Frontend__Url` environment variable in App Service matches your Static Web App URL exactly (including https://).

## Additional Resources

- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review Azure Portal diagnostics and logs
3. Open an issue in the project repository
