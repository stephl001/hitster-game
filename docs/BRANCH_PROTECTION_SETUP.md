# Branch Protection Rules Setup

This document provides instructions for configuring branch protection rules on GitHub to support the branching model for this project.

## Branch Protection Configuration

### Main Branch (Production)

1. Navigate to **Settings** > **Branches** in your GitHub repository
2. Click **Add rule** or edit the existing rule for `main`
3. Configure the following settings:

**Branch name pattern:** `main`

**Protect matching branches:**
- ✅ Require a pull request before merging
  - ✅ Require approvals: **1** (recommended)
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  - ✅ Require review from Code Owners (optional)
- ✅ Require status checks to pass before merging
  - ✅ Require branches to be up to date before merging
  - Add required status checks:
    - `deploy-backend` (from deploy-dev workflow)
    - `deploy-frontend` (from deploy-dev workflow)
- ✅ Require conversation resolution before merging
- ✅ Require signed commits (optional but recommended)
- ✅ Require linear history (recommended)
- ✅ Include administrators (recommended for consistency)
- ❌ Allow force pushes (keep disabled)
- ❌ Allow deletions (keep disabled)

### Develop Branch (Development)

1. Navigate to **Settings** > **Branches** in your GitHub repository
2. Click **Add rule**
3. Configure the following settings:

**Branch name pattern:** `develop`

**Protect matching branches:**
- ✅ Require a pull request before merging
  - Require approvals: **0** or **1** (flexible for dev environment)
- ✅ Require status checks to pass before merging (optional)
  - Add build/test checks if you have them
- ✅ Require conversation resolution before merging (optional)
- ✅ Require linear history (recommended)
- ❌ Allow force pushes (keep disabled)
- ❌ Allow deletions (keep disabled)

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
   - **Deployment protection rules:**
     - ✅ Required reviewers: Add yourself or team members (recommended)
     - ✅ Wait timer: 0 minutes (or add a delay if desired)
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

After setting up the branch protection rules:

1. Try pushing directly to `main` - should be rejected
2. Try pushing directly to `develop` - should be rejected
3. Create a feature branch, make changes, and open a PR to `develop` - should succeed
4. Merge PR to `develop` - should trigger dev deployment workflow
5. Open PR from `develop` to `main` - should require approval
6. Merge to `main` - should trigger prod deployment workflow

## Notes

- These settings enforce code review and prevent accidental direct commits to protected branches
- The `main` branch has stricter rules because it deploys to production
- Administrators are included in protection rules to maintain consistency
- You can adjust the number of required reviewers based on team size
