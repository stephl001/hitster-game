# Trunk-Based Development - Quick Start Guide

This is a quick reference for the trunk-based development workflow used in this project.

## Daily Workflow

### 1. Make Changes
```bash
# Make sure you're on main
git checkout main
git pull origin main

# Make your changes to the code
# Edit files, add features, fix bugs, etc.
```

### 2. Commit and Deploy to Dev
```bash
# Stage and commit your changes
git add .
git commit -m "Add new feature"

# Push to main → automatically deploys to DEV
git push origin main
```

**Result:** Only the changed component is deployed to the **dev environment**:
- If you changed **backend** files → Backend deploys to `https://app-songster-api-dev.azurewebsites.net`
- If you changed **frontend** files → Frontend deploys to `https://stapp-songster-web-dev.azurewebsites.net`
- If you changed **both** → Both deploy (separate workflows run in parallel)

### 3. Test on Dev Environment
- Open the dev frontend URL in your browser
- Test your changes
- If issues are found, fix them and push again (repeat step 2)

### 4. Deploy to Production (When Ready)
```bash
# Tag the current commit with a version number
git tag -a v1.0.0 -m "Initial production release"

# Push the tag → automatically deploys to PROD
git push origin v1.0.0
```

**Result:** Both backend and frontend are deployed to the **prod environment** (parallel workflows):
- Backend: `https://app-songster-api-prod.azurewebsites.net`
- Frontend: `https://stapp-songster-web-prod.azurewebsites.net` (check Azure portal for actual URL)

**Note:** Production tags always deploy both backend and frontend, regardless of what changed.

## Version Tagging Guide

Use **semantic versioning** for your releases:

### Format: `vMAJOR.MINOR.PATCH`

```bash
# First release
git tag -a v1.0.0 -m "Initial production release"

# New feature (backward compatible)
git tag -a v1.1.0 -m "Add WebRTC audio streaming"

# Bug fix
git tag -a v1.0.1 -m "Fix timeline placement bug"

# Major version (breaking changes)
git tag -a v2.0.0 -m "Complete UI redesign"
```

### View Existing Tags
```bash
# List all tags
git tag -l

# List tags with messages
git tag -l -n
```

### Delete a Tag (if needed)
```bash
# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin --delete v1.0.0
```

## Rollback Production

If a production release has issues:

```bash
# Option 1: Re-push a previous good tag
git push origin v1.0.0
# This re-triggers the prod deployment with v1.0.0

# Option 2: Create a new patch version
# Fix the issue on main first
git tag -a v1.0.2 -m "Fix critical bug from v1.0.1"
git push origin v1.0.2
```

## Emergency Hotfix (Rare)

Only use this if you need to work on a fix without deploying to dev first:

```bash
# Create hotfix branch
git checkout -b hotfix/critical-security-issue

# Make the fix
git add .
git commit -m "Fix critical security issue"

# Push and create PR to main
git push -u origin hotfix/critical-security-issue
# Open PR on GitHub, review, and merge

# After merging to main, tag for prod deployment
git checkout main
git pull origin main
git tag -a v1.0.1 -m "Hotfix: Security issue"
git push origin v1.0.1
```

## Checking Deployment Status

### On GitHub
1. Go to your repository on GitHub
2. Click **Actions** tab
3. See the latest workflow runs:
   - "Deploy Backend to Dev" - triggered by backend changes on main
   - "Deploy Frontend to Dev" - triggered by frontend changes on main
   - "Deploy Backend to Production" - triggered by version tags
   - "Deploy Frontend to Production" - triggered by version tags

### Via Azure Portal
1. Go to Azure Portal
2. Find your App Service or Static Web App
3. Check deployment history and logs

## Common Commands Cheat Sheet

```bash
# Daily work
git pull origin main              # Get latest changes
git add .                         # Stage changes
git commit -m "Description"       # Commit changes
git push origin main              # Deploy to dev

# Production release
git tag -a v1.0.0 -m "Message"    # Create version tag
git push origin v1.0.0            # Deploy to prod

# Utilities
git status                        # Check current status
git log --oneline -10             # View recent commits
git tag -l                        # List all tags
git diff                          # See unstaged changes
```

## Important Notes

- ✅ **Commit directly to main** - No feature branches needed for solo dev
- ✅ **Every push to main deploys to dev** - Test your changes there
- ✅ **Tags deploy to prod** - Use semantic versioning
- ⚠️ **Test on dev first** - Don't tag for prod until you've verified on dev
- ⚠️ **Tags are permanent** - Choose version numbers carefully

## Need Help?

- **Full documentation:** See `CLAUDE.md` for complete branching model docs
- **Setup instructions:** See `docs/BRANCH_PROTECTION_SETUP.md`
- **Detailed workflow:** See `docs/BRANCHING_MODEL_SUMMARY.md`
