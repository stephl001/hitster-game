# Deployment Guide - Songster Game

This guide provides step-by-step instructions for deploying the Songster Game to Azure.

## Quick Start

```bash
# 1. Deploy infrastructure
cd infrastructure/scripts
./deploy-dev.sh  # or .\deploy-dev.ps1 on Windows

# 2. Add Spotify secrets (see below)

# 3. Deploy backend
cd ../../backend/SongsterGame.Api
dotnet publish -c Release
# Deploy to Azure (see detailed steps below)

# 4. Deploy frontend
cd ../../frontend
npm run build
# Deploy to Azure (see detailed steps below)
```

## Prerequisites

Before you begin, ensure you have:

1. **Azure Subscription** - [Sign up for free](https://azure.microsoft.com/free/)
2. **Azure CLI** - [Installation guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
3. **Node.js 20+** and npm
4. **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download)
5. **Spotify Developer Account** - [Sign up](https://developer.spotify.com/dashboard)
6. **Git** installed

## Step 1: Prepare Spotify API Credentials

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Click "Create App"
3. Fill in the details:
   - **App Name**: Songster Game Dev
   - **App Description**: Music timeline game
   - **Redirect URI**: `https://app-songster-api-dev.azurewebsites.net/api/auth/callback`
   - **API/SDK**: Web API
4. Save your **Client ID** and **Client Secret**

## Step 2: Deploy Azure Infrastructure

### Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

### Deploy using scripts

**On Windows:**
```powershell
cd infrastructure\scripts
.\deploy-dev.ps1
```

**On Linux/Mac:**
```bash
cd infrastructure/scripts
chmod +x deploy-dev.sh
./deploy-dev.sh
```

The script will:
- Create resource group `rg-songster-dev`
- Deploy all Azure resources (App Service, Static Web App, Key Vault, etc.)
- Output deployment information
- Save outputs to `.azure/deployment-outputs-dev.json`

**Save the following from the output:**
- Key Vault name (e.g., `kv-songster-dev-abc123xyz`)
- App Service name (e.g., `app-songster-api-dev`)
- Static Web App deployment token

## Step 3: Configure Secrets

Add your Spotify credentials to Azure Key Vault:

```bash
# Replace with your Key Vault name from Step 2
KEY_VAULT_NAME="kv-songster-dev-XXXXXX"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name SpotifyClientId \
  --value "YOUR_SPOTIFY_CLIENT_ID"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name SpotifyClientSecret \
  --value "YOUR_SPOTIFY_CLIENT_SECRET"
```

## Step 4: Deploy Backend API

### Option A: Using Azure CLI (Recommended for first deployment)

```bash
cd backend/SongsterGame.Api

# Publish the application
dotnet publish -c Release -o ./publish

# Create ZIP package
cd publish
zip -r ../publish.zip .  # On Linux/Mac
# On Windows: Compress-Archive -Path * -DestinationPath ..\publish.zip
cd ..

# Deploy to Azure
az webapp deploy \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev \
  --src-path ./publish.zip \
  --type zip

# Restart the app service
az webapp restart \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

### Option B: Using Visual Studio / VS Code

1. Right-click on `SongsterGame.Api` project
2. Select "Publish"
3. Choose "Azure" → "Azure App Service (Linux)"
4. Select `app-songster-api-dev`
5. Click "Publish"

### Verify Backend Deployment

```bash
# Check if backend is running
curl https://app-songster-api-dev.azurewebsites.net/health

# View logs
az webapp log tail \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

## Step 5: Deploy Frontend

### Option A: Using GitHub Actions (Recommended)

1. **Set up GitHub secrets** (Repository Settings → Secrets and variables → Actions):
   ```
   AZURE_STATIC_WEB_APP_API_TOKEN: [from deployment output]
   SPOTIFY_CLIENT_ID: [your Spotify client ID]
   ```

2. **Push to main branch** - The workflow will automatically deploy

### Option B: Manual Deployment with Azure CLI

```bash
cd frontend

# Install dependencies
npm ci

# Build with environment variables
export VITE_API_URL=https://app-songster-api-dev.azurewebsites.net
export VITE_SPOTIFY_CLIENT_ID=your_spotify_client_id
npm run build

# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Get deployment token
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name stapp-songster-web-dev \
  --resource-group rg-songster-dev \
  --query properties.apiKey -o tsv)

# Deploy
swa deploy ./dist --deployment-token $DEPLOYMENT_TOKEN
```

### Verify Frontend Deployment

Open your browser to: `https://stapp-songster-web-dev.azurestaticapps.net`

## Step 6: Test the Application

1. **Open the frontend URL** in your browser
2. **Create a game** - You should be redirected to Spotify for authentication
3. **Join with multiple players** - Open the app in different browser tabs/devices
4. **Start playing!**

## Environment Variables Reference

### Backend (App Service)

Set via Azure Portal or CLI:

| Variable | Value | Source |
|----------|-------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Auto-configured |
| `Spotify__ClientId` | `[from Key Vault]` | Auto-configured |
| `Spotify__ClientSecret` | `[from Key Vault]` | Auto-configured |
| `Spotify__RedirectUri` | `https://app-songster-api-dev.azurewebsites.net/api/auth/callback` | Parameters |
| `Frontend__Url` | `https://stapp-songster-web-dev.azurestaticapps.net` | Parameters |

### Frontend (Build-time)

Set during `npm run build`:

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | `https://app-songster-api-dev.azurewebsites.net` |
| `VITE_SPOTIFY_CLIENT_ID` | `[your Spotify client ID]` |

## GitHub Actions CI/CD Setup

### Configure Azure Credentials

For secure GitHub Actions deployments, set up federated identity:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name sp-songster-github \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-songster-dev \
  --json-auth

# Output will contain: clientId, tenantId, subscriptionId
```

### Add GitHub Secrets

Go to GitHub repository → Settings → Secrets and variables → Actions:

```
AZURE_CLIENT_ID: [from service principal output]
AZURE_TENANT_ID: [from service principal output]
AZURE_SUBSCRIPTION_ID: [your subscription ID]
AZURE_STATIC_WEB_APP_API_TOKEN: [from infrastructure deployment]
SPOTIFY_CLIENT_ID: [your Spotify client ID]
```

### Trigger Deployments

- **Infrastructure**: Go to Actions → "Deploy Azure Infrastructure" → Run workflow
- **Backend**: Push changes to `backend/` directory on `main` or `dev` branch
- **Frontend**: Push changes to `frontend/` directory on `main` or `dev` branch

## Troubleshooting

### Backend not responding (503 error)

**Cause**: F1 App Service has cold starts (60 CPU min/day limit)

**Solutions**:
1. Wait 30-60 seconds for the app to warm up
2. Check if you've exceeded 60 CPU minutes for the day
3. Upgrade to B1 tier for "Always On" capability

```bash
# Check App Service status
az webapp show \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev \
  --query state

# View logs
az webapp log tail \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

### CORS errors in browser console

**Cause**: Frontend URL not properly configured in backend

**Solution**:
```bash
# Update CORS settings
az webapp config appsettings set \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev \
  --settings Frontend__Url=https://stapp-songster-web-dev.azurestaticapps.net

# Restart app
az webapp restart \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

### Spotify authentication fails

**Causes**:
1. Incorrect redirect URI in Spotify app settings
2. Secrets not properly set in Key Vault
3. App Service can't access Key Vault

**Solutions**:

1. **Verify Spotify redirect URI** matches exactly:
   ```
   https://app-songster-api-dev.azurewebsites.net/api/auth/callback
   ```

2. **Check Key Vault secrets**:
   ```bash
   az keyvault secret list --vault-name kv-songster-dev-XXXXX
   ```

3. **Verify App Service Managed Identity has access**:
   ```bash
   az keyvault set-policy \
     --name kv-songster-dev-XXXXX \
     --object-id $(az webapp identity show \
       --resource-group rg-songster-dev \
       --name app-songster-api-dev \
       --query principalId -o tsv) \
     --secret-permissions get list
   ```

### Frontend build fails

**Cause**: Missing environment variables

**Solution**: Ensure `VITE_API_URL` is set:
```bash
export VITE_API_URL=https://app-songster-api-dev.azurewebsites.net
npm run build
```

## Monitoring and Logs

### View Application Insights

```bash
# Open Application Insights in Azure Portal
az monitor app-insights show \
  --app appi-songster-dev \
  --resource-group rg-songster-dev

# Query logs
az monitor app-insights query \
  --app appi-songster-dev \
  --resource-group rg-songster-dev \
  --analytics-query "traces | where severityLevel > 1 | take 20"
```

### Stream Backend Logs

```bash
az webapp log tail \
  --resource-group rg-songster-dev \
  --name app-songster-api-dev
```

## Cost Management

### Current Setup (Free Tier)
- **App Service (F1)**: $0/month
- **Static Web App (Free)**: $0/month
- **Application Insights**: $0/month (within free 5GB limit)
- **Key Vault**: ~$0/month (minimal operations)
- **Total**: ~$0/month

### Limitations of Free Tier
- 60 CPU minutes per day (resets midnight UTC)
- Cold starts (no Always On)
- Single instance only
- Perfect for development, not production

### View Current Costs

```bash
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31
```

## Cleanup

To delete all resources and stop incurring charges:

```bash
az group delete \
  --name rg-songster-dev \
  --yes \
  --no-wait
```

## Next Steps

1. **Set up staging environment** - Duplicate infrastructure with `staging.parameters.json`
2. **Configure custom domain** - Add custom domain to Static Web App (requires Standard SKU)
3. **Set up monitoring alerts** - Configure Application Insights alerts
4. **Implement database** - Add Azure SQL or Cosmos DB for game persistence
5. **Upgrade to production** - Use B1 or higher tier for production workloads

## Support

- **Documentation**: See `infrastructure/README.md` for detailed infrastructure docs
- **Issues**: Report issues in the project repository
- **Azure Support**: [Azure Support Portal](https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade)
