# Branch Protection Rules Setup (Optional)

This document provides instructions for configuring branch protection rules on GitHub for trunk-based development. **Note: For solo development, branch protection is optional** but can help prevent accidental force pushes.

## Branch Protection Configuration

### Main Branch (Trunk)

1. Navigate to **Settings** > **Branches** in your GitHub repository
2. Click **Add rule** or edit the existing rule for `main`
3. Configure the following settings:

**Branch name pattern:** `main`

**Protect matching branches (minimal protection for solo dev):**
- ❌ Require a pull request before merging (disabled - need direct push for trunk-based)
- ⚠️ Require status checks to pass before merging (optional)
  - Add required status checks if desired:
    - `deploy-backend` (from deploy-dev workflow)
    - `deploy-frontend` (from deploy-dev workflow)
- ✅ Require signed commits (optional but recommended)
- ✅ Require linear history (recommended - keeps history clean)
- ❌ Include administrators (disabled - allows you to bypass rules when needed)
- ❌ Allow force pushes (keep disabled - prevents accidental overwrites)
- ❌ Allow deletions (keep disabled - prevents accidental branch deletion)

**Recommended minimal setup for solo development:**
- Only enable "Require linear history" and "Block force pushes"
- Keep everything else disabled to maintain workflow flexibility

## GitHub Environments Setup

You also need to configure GitHub Environments to match the deployment workflows:

### Dev Environment

1. Navigate to **Settings** > **Environments**
2. Click **New environment** and name it `dev`
3. Configure:
   - **Deployment protection rules:** None required (auto-deploy on develop branch)
   - **Environment secrets:**
     - Add any dev-specific secrets (e.g., `SPOTIFY_CLIENT_ID` for dev)

### Prod Environment

1. Navigate to **Settings** > **Environments**
2. Click **New environment** and name it `prod`
3. Configure:
   - **Deployment protection rules (optional for solo dev):**
     - Required reviewers: Optional (you're the only developer)
     - Wait timer: Optional (can add a delay if you want time to cancel)
   - **Environment secrets:**
     - Add any prod-specific secrets (e.g., `SPOTIFY_CLIENT_ID` for prod)
     - Add `AZURE_STATIC_WEB_APP_API_TOKEN_PROD` (deployment token for prod Static Web App)

## Required GitHub Secrets

Ensure these secrets are configured at the **repository level**:

### Repository Secrets

Navigate to **Settings** > **Secrets and variables** > **Actions** > **New repository secret**

- `AZURE_CREDENTIALS` - Azure service principal credentials (JSON format)
  ```json
  {
    "clientId": "...",
    "clientSecret": "...",
    "subscriptionId": "...",
    "tenantId": "..."
  }
  ```
- `AZURE_STATIC_WEB_APP_API_TOKEN` - Deployment token for dev Static Web App
- `AZURE_STATIC_WEB_APP_API_TOKEN_PROD` - Deployment token for prod Static Web App
- `SPOTIFY_CLIENT_ID` - Spotify application client ID (can be environment-specific)
- `GH_PAT` - GitHub Personal Access Token with `repo` scope (for auto-configuring secrets)

## Verification

After setting up (minimal branch protection is optional):

1. Make a change and commit to `main` - should succeed (direct push allowed)
2. Push to `main` - should trigger dev deployment workflow
3. Create a version tag (e.g., `v0.1.0`) and push - should trigger prod deployment workflow
4. Try force pushing to `main` - should be rejected (if force push protection enabled)

## Notes for Trunk-Based Development

- **Branch protection is optional** for solo development
- If using protection, keep it minimal to maintain workflow flexibility
- **Do NOT enable "Require pull request"** - this breaks trunk-based development
- Linear history and force push blocking are the most useful protections
- You can always adjust settings later as needs change
