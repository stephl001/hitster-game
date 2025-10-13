# deploy-dev.ps1 - Deploy Songster Game infrastructure to Azure (Dev environment)
# This script deploys the infrastructure using Azure Bicep templates

# Configuration
$ResourceGroup = "rg-songster-dev"
$Location = "canadacentral"
$Environment = "dev"
$BicepFile = "..\bicep\main.bicep"
$ParametersFile = "..\bicep\parameters\dev.parameters.json"

Write-Host "========================================" -ForegroundColor Green
Write-Host "Songster Game - Azure Infrastructure Deployment" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Azure CLI is not installed" -ForegroundColor Red
    Write-Host "Please install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if user is logged in
Write-Host "Checking Azure CLI authentication..." -ForegroundColor Yellow
try {
    az account show | Out-Null
} catch {
    Write-Host "Not logged in to Azure CLI" -ForegroundColor Red
    Write-Host "Running 'az login'..."
    az login
}

# Display current subscription
$SubscriptionName = az account show --query name -o tsv
$SubscriptionId = az account show --query id -o tsv
Write-Host "Using subscription: $SubscriptionName ($SubscriptionId)" -ForegroundColor Green
Write-Host ""

# Create resource group if it doesn't exist
Write-Host "Creating resource group: $ResourceGroup" -ForegroundColor Yellow
az group create `
    --name $ResourceGroup `
    --location $Location `
    --tags Environment=$Environment Project=Songster ManagedBy=Bicep
Write-Host "✓ Resource group ready" -ForegroundColor Green
Write-Host ""

# Build Bicep template (validates syntax)
Write-Host "Building and validating Bicep template..." -ForegroundColor Yellow
$BicepBuildResult = az bicep build --file $BicepFile 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Bicep build failed:" -ForegroundColor Red
    Write-Host $BicepBuildResult -ForegroundColor Red
    exit 1
}

Write-Host "✓ Bicep template is valid" -ForegroundColor Green
Write-Host ""

# Deploy infrastructure
Write-Host "Deploying infrastructure (this may take 5-10 minutes)..." -ForegroundColor Yellow
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$DeploymentName = "songster-$Environment-$Timestamp"

Write-Host "Deployment details:" -ForegroundColor Cyan
Write-Host "  Resource Group: $ResourceGroup" -ForegroundColor Cyan
Write-Host "  Template File: $BicepFile" -ForegroundColor Cyan
Write-Host "  Parameters File: $ParametersFile" -ForegroundColor Cyan
Write-Host "  Deployment Name: $DeploymentName" -ForegroundColor Cyan
Write-Host ""

# Save deployment output to file for debugging
$DeploymentLogFile = "..\..\\.azure\deployment-log-$Timestamp.txt"
Write-Host "Deployment output will be saved to: $DeploymentLogFile" -ForegroundColor Cyan
Write-Host ""

try {
    # Run deployment and capture output to file
    Write-Host "Running deployment command..." -ForegroundColor Cyan

    # Use Start-Process to properly capture output and error streams
    $TempOutputFile = [System.IO.Path]::GetTempFileName()
    $TempErrorFile = [System.IO.Path]::GetTempFileName()

    $ProcessArgs = @(
        "deployment", "group", "create",
        "--resource-group", $ResourceGroup,
        "--template-file", $BicepFile,
        "--parameters", $ParametersFile,
        "--name", $DeploymentName,
        "--output", "json"
    )

    $Process = Start-Process -FilePath "az" -ArgumentList $ProcessArgs `
        -RedirectStandardOutput $TempOutputFile `
        -RedirectStandardError $TempErrorFile `
        -NoNewWindow -Wait -PassThru

    $ExitCode = $Process.ExitCode
    $StdOut = Get-Content $TempOutputFile -Raw
    $StdErr = Get-Content $TempErrorFile -Raw

    # Save full output to log
    @"
Exit Code: $ExitCode

=== STDOUT ===
$StdOut

=== STDERR ===
$StdErr
"@ | Set-Content $DeploymentLogFile

    # Clean up temp files
    Remove-Item $TempOutputFile, $TempErrorFile -ErrorAction SilentlyContinue

    Write-Host "Deployment command completed with exit code: $ExitCode" -ForegroundColor $(if ($ExitCode -eq 0) { "Green" } else { "Red" })

    if ($ExitCode -ne 0) {
        Write-Host ""
        Write-Host "Error output:" -ForegroundColor Red
        Write-Host $StdErr -ForegroundColor Red
        throw "Deployment command failed with exit code $ExitCode"
    }

    # Parse JSON output
    if ($StdOut) {
        $DeploymentOutput = $StdOut | ConvertFrom-Json
    } else {
        throw "No deployment output received"
    }
}
catch {
    Write-Host "✗ Infrastructure deployment failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Deployment log saved to: $DeploymentLogFile" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Checking deployment operation details..." -ForegroundColor Yellow

    # Try to get detailed error information
    $DeploymentExists = az deployment group show --resource-group $ResourceGroup --name $DeploymentName 2>$null
    if ($DeploymentExists) {
        az deployment operation group list `
            --resource-group $ResourceGroup `
            --name $DeploymentName `
            --query "[?properties.provisioningState=='Failed'].{Resource:properties.targetResource.resourceName, Error:properties.statusMessage.error}" `
            --output json | ConvertFrom-Json | Format-List
    } else {
        Write-Host "Deployment not found in Azure. Check the error output above for validation errors." -ForegroundColor Yellow
    }

    exit 1
}

Write-Host "✓ Infrastructure deployment completed" -ForegroundColor Green
Write-Host ""

# Extract outputs
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Outputs" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

$AppServiceName = $DeploymentOutput.properties.outputs.appServiceName.value
$AppServiceUrl = $DeploymentOutput.properties.outputs.appServiceUrl.value
$KeyVaultName = $DeploymentOutput.properties.outputs.keyVaultName.value
$StaticWebAppName = $DeploymentOutput.properties.outputs.staticWebAppName.value
$StaticWebAppUrl = $DeploymentOutput.properties.outputs.staticWebAppUrl.value

# Retrieve Static Web App deployment token separately (not available via Bicep output for security)
Write-Host "Retrieving Static Web App deployment token..." -ForegroundColor Yellow
$StaticWebAppToken = az staticwebapp secrets list --name $StaticWebAppName --query "properties.apiKey" -o tsv

Write-Host "Backend API:"
Write-Host "  Name: $AppServiceName"
Write-Host "  URL: $AppServiceUrl"
Write-Host ""
Write-Host "Frontend:"
Write-Host "  Name: $StaticWebAppName"
Write-Host "  URL: $StaticWebAppUrl"
Write-Host ""
Write-Host "Key Vault:"
Write-Host "  Name: $KeyVaultName"
Write-Host ""

# Save outputs to file
$OutputFile = "..\..\\.azure\deployment-outputs-$Environment.json"
New-Item -ItemType Directory -Force -Path "..\..\\.azure" | Out-Null
$DeploymentOutput | ConvertTo-Json -Depth 10 | Set-Content $OutputFile
Write-Host "Deployment outputs saved to: $OutputFile" -ForegroundColor Green
Write-Host ""

# Next steps
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Next Steps" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Add Spotify secrets to Key Vault:"
Write-Host "   az keyvault secret set --vault-name `"$KeyVaultName`" --name `"SpotifyClientId`" --value `"YOUR_SPOTIFY_CLIENT_ID`""
Write-Host "   az keyvault secret set --vault-name `"$KeyVaultName`" --name `"SpotifyClientSecret`" --value `"YOUR_SPOTIFY_CLIENT_SECRET`""
Write-Host ""
Write-Host "2. Configure GitHub secrets for CI/CD:"
Write-Host "   AZURE_STATIC_WEB_APP_API_TOKEN: $StaticWebAppToken"
Write-Host "   (Save this token - it won't be shown again!)"
Write-Host ""
Write-Host "3. Deploy backend application:"
Write-Host "   cd ..\..\backend\SongsterGame.Api"
Write-Host "   dotnet publish -c Release"
Write-Host "   az webapp deploy --resource-group `"$ResourceGroup`" --name `"$AppServiceName`" --src-path .\bin\Release\net9.0\publish.zip"
Write-Host ""
Write-Host "4. Deploy frontend application:"
Write-Host "   cd ..\..\frontend"
Write-Host "   npm run build"
Write-Host "   (Use GitHub Actions or Azure Static Web Apps CLI)"
Write-Host ""
Write-Host "Deployment complete! 🎉" -ForegroundColor Green
