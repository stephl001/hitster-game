# Quick Deployment Instructions

## Step 1: Add GitHub Secret

1. Open: https://github.com/stephl001/hitster-game/settings/secrets/actions

2. Click **"New repository secret"**

3. Create secret:
   - **Name**: `AZURE_CREDENTIALS`
   - **Value**: Copy from `.azure/GITHUB_SECRETS.txt` file

4. Click **"Add secret"**

## Step 2: Run GitHub Actions Deployment

1. Go to: https://github.com/stephl001/hitster-game/actions

2. Click **"Deploy Azure Infrastructure"** in the left sidebar

3. Click **"Run workflow"** button (top right)

4. Select:
   - **Branch**: `feature/azure-infrastructure-dev`
   - **Environment**: `dev`

5. Click **"Run workflow"**

6. Watch the deployment progress (takes ~5-10 minutes)

## Step 3: After Deployment

Once the GitHub Action completes successfully:

1. Check the workflow summary for output URLs:
   - Backend API URL
   - Frontend URL
   - Key Vault name

2. Add Spotify credentials to Key Vault:
   ```bash
   az keyvault secret set \
     --vault-name <KEY_VAULT_NAME_FROM_OUTPUT> \
     --name SpotifyClientId \
     --value "YOUR_SPOTIFY_CLIENT_ID"

   az keyvault secret set \
     --vault-name <KEY_VAULT_NAME_FROM_OUTPUT> \
     --name SpotifyClientSecret \
     --value "YOUR_SPOTIFY_CLIENT_SECRET"
   ```

3. Deploy backend application (see DEPLOYMENT.md)

4. Deploy frontend application (see DEPLOYMENT.md)

## Resources Created

The deployment will create:
- Resource Group: `rg-songster-dev`
- App Service: `app-songster-api-dev` (F1 Free tier)
- Static Web App: `stapp-songster-web-dev` (Free tier)
- Key Vault: `kv-songster-dev-XXXXXX` (unique suffix)
- Application Insights: `appi-songster-dev`
- Log Analytics: `log-songster-dev`

**Total Cost**: ~$0/month (all free tier)

## Troubleshooting

If deployment fails:
1. Check the Actions logs for specific errors
2. Verify the AZURE_CREDENTIALS secret is correct
3. Ensure the service principal has Contributor role on the resource group
4. Check that the resource group `rg-songster-dev` exists

## Next Steps

After infrastructure is deployed, see:
- `DEPLOYMENT.md` - Full deployment guide
- `infrastructure/README.md` - Infrastructure documentation
