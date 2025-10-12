#!/bin/bash

# deploy-dev.sh - Deploy Songster Game infrastructure to Azure (Dev environment)
# This script deploys the infrastructure using Azure Bicep templates

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="rg-songster-dev"
LOCATION="eastus"
ENVIRONMENT="dev"
BICEP_FILE="../bicep/main.bicep"
PARAMETERS_FILE="../bicep/parameters/dev.parameters.json"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Songster Game - Azure Infrastructure Deployment${NC}"
echo -e "${GREEN}Environment: ${ENVIRONMENT}${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in
echo -e "${YELLOW}Checking Azure CLI authentication...${NC}"
az account show &> /dev/null || {
    echo -e "${RED}Not logged in to Azure CLI${NC}"
    echo "Running 'az login'..."
    az login
}

# Display current subscription
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo -e "${GREEN}Using subscription: ${SUBSCRIPTION_NAME} (${SUBSCRIPTION_ID})${NC}"
echo ""

# Create resource group if it doesn't exist
echo -e "${YELLOW}Creating resource group: ${RESOURCE_GROUP}${NC}"
az group create \
    --name "${RESOURCE_GROUP}" \
    --location "${LOCATION}" \
    --tags Environment="${ENVIRONMENT}" Project="Songster" ManagedBy="Bicep"
echo -e "${GREEN}✓ Resource group ready${NC}"
echo ""

# Validate Bicep template
echo -e "${YELLOW}Validating Bicep template...${NC}"
az deployment group validate \
    --resource-group "${RESOURCE_GROUP}" \
    --template-file "${BICEP_FILE}" \
    --parameters "${PARAMETERS_FILE}" \
    > /dev/null
echo -e "${GREEN}✓ Bicep template validation passed${NC}"
echo ""

# Deploy infrastructure
echo -e "${YELLOW}Deploying infrastructure (this may take 5-10 minutes)...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "${RESOURCE_GROUP}" \
    --template-file "${BICEP_FILE}" \
    --parameters "${PARAMETERS_FILE}" \
    --name "songster-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)" \
    --output json)

echo -e "${GREEN}✓ Infrastructure deployment completed${NC}"
echo ""

# Extract outputs
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Outputs${NC}"
echo -e "${GREEN}========================================${NC}"

APP_SERVICE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.appServiceName.value')
APP_SERVICE_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.appServiceUrl.value')
KEY_VAULT_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.keyVaultName.value')
STATIC_WEB_APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.staticWebAppName.value')
STATIC_WEB_APP_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.staticWebAppUrl.value')
STATIC_WEB_APP_TOKEN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.staticWebAppDeploymentToken.value')

echo "Backend API:"
echo "  Name: ${APP_SERVICE_NAME}"
echo "  URL: ${APP_SERVICE_URL}"
echo ""
echo "Frontend:"
echo "  Name: ${STATIC_WEB_APP_NAME}"
echo "  URL: ${STATIC_WEB_APP_URL}"
echo ""
echo "Key Vault:"
echo "  Name: ${KEY_VAULT_NAME}"
echo ""

# Save outputs to file
OUTPUT_FILE="../../.azure/deployment-outputs-${ENVIRONMENT}.json"
mkdir -p "../../.azure"
echo "$DEPLOYMENT_OUTPUT" > "$OUTPUT_FILE"
echo -e "${GREEN}Deployment outputs saved to: ${OUTPUT_FILE}${NC}"
echo ""

# Next steps
echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}Next Steps${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""
echo "1. Add Spotify secrets to Key Vault:"
echo "   az keyvault secret set --vault-name \"${KEY_VAULT_NAME}\" --name \"SpotifyClientId\" --value \"YOUR_SPOTIFY_CLIENT_ID\""
echo "   az keyvault secret set --vault-name \"${KEY_VAULT_NAME}\" --name \"SpotifyClientSecret\" --value \"YOUR_SPOTIFY_CLIENT_SECRET\""
echo ""
echo "2. Configure GitHub secrets for CI/CD:"
echo "   AZURE_STATIC_WEB_APP_API_TOKEN: ${STATIC_WEB_APP_TOKEN}"
echo "   (Save this token - it won't be shown again!)"
echo ""
echo "3. Deploy backend application:"
echo "   cd ../../backend/SongsterGame.Api"
echo "   dotnet publish -c Release"
echo "   az webapp deploy --resource-group \"${RESOURCE_GROUP}\" --name \"${APP_SERVICE_NAME}\" --src-path ./bin/Release/net9.0/publish.zip"
echo ""
echo "4. Deploy frontend application:"
echo "   cd ../../frontend"
echo "   npm run build"
echo "   (Use GitHub Actions or Azure Static Web Apps CLI)"
echo ""
echo -e "${GREEN}Deployment complete! 🎉${NC}"
