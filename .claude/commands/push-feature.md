---
description: Safely rebase current feature branch onto main and push to remote
---

Push the current feature branch to main by performing the following steps:

1. **Pre-flight Checks:**
   - Verify we are NOT on the main branch (abort if on main)
   - Check for uncommitted changes (abort if there are any)
   - Verify the main branch exists
   - Store the current branch name for later deletion

2. **Rebase onto Main:**
   - Switch to the main branch
   - Pull latest changes from origin/main to ensure we're up to date
   - Switch back to the feature branch
   - Rebase the feature branch commits onto main
   - If rebase conflicts occur, abort and inform the user

3. **Merge to Main:**
   - Switch to main branch
   - Merge the feature branch (should be a fast-forward merge after rebase)
   - Verify the merge succeeded

4. **Push to Remote:**
   - Push the main branch to origin
   - Verify the push succeeded

5. **Cleanup:**
   - Delete the local feature branch (since it's now merged)
   - Optionally delete the remote feature branch if it exists

6. **Summary:**
   - Display a summary of what was done
   - Show the current branch (should be main)
   - List recent commits on main

**Safety Rules:**
- NEVER proceed if on main branch
- NEVER proceed if there are uncommitted changes
- NEVER force push
- ALWAYS abort on rebase conflicts (don't auto-resolve)
- ALWAYS verify each step succeeded before proceeding

**Expected Workflow:**
```
feature/my-feature (current)
  � rebase onto main
main (with feature commits)
  � push to origin/main
origin/main (updated)
  � cleanup
feature/my-feature (deleted)
```

If any step fails, stop immediately and report the error to the user without proceeding further.
