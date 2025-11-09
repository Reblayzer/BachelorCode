# CI/CD Documentation

## Overview

StorageConnector uses **GitHub Actions** for automated continuous integration and continuous deployment (CI/CD). This ensures code quality, automated testing, and streamlined releases.

## Workflows

### 1. CI/CD Pipeline (`ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches

**Jobs:**

#### Backend Build & Test
- Restores NuGet dependencies
- Builds solution in Release configuration
- Runs all tests (unit + integration)
- Publishes IdentityService and LinkingService
- Uploads artifacts for deployment

#### Frontend Build & Test
- Installs npm dependencies using `npm ci`
- Runs TypeScript linter
- Executes Vitest tests
- Builds production bundle
- Uploads frontend artifact

#### Code Quality
- Validates C# code formatting using `dotnet format`
- Runs security scans

#### Build Summary
- Aggregates results from all jobs
- Fails if any critical job fails
- Provides clear success/failure indication

**Artifacts Produced:**
- `IdentityService` - Published .NET service
- `LinkingService` - Published .NET service
- `Frontend` - Production-ready React bundle
- `backend-test-results` - Test result files

### 2. Release Workflow (`release.yml`)

**Trigger:**
- Pushing a version tag (e.g., `v1.0.0`, `v2.1.3`)

**Process:**
1. Builds all services in Release mode
2. Publishes backend services
3. Builds frontend bundle
4. Creates ZIP archives for each component
5. Creates GitHub Release with:
   - Release notes
   - Download links
   - Service archives as assets

**Usage:**
```bash
# Tag a new version
git tag v1.0.0

# Push the tag to trigger release
git push origin v1.0.0
```

**Artifacts:**
- `IdentityService-v1.0.0.zip`
- `LinkingService-v1.0.0.zip`
- `Frontend-v1.0.0.zip`

### 3. Security Scanning (`security.yml`)

**Triggers:**
- Scheduled: Every Monday at 9:00 AM UTC
- Manual: Via "Run workflow" button in GitHub Actions

**Checks:**
- **Dependency Review:**
  - Lists outdated NuGet packages
  - Scans for vulnerable NuGet packages
  - Checks outdated npm packages
  - Runs npm security audit

- **CodeQL Analysis:**
  - Static code analysis for C# and JavaScript
  - Identifies security vulnerabilities
  - Reports potential issues in Security tab

## GitHub Actions Permissions

The workflows require the following permissions:
- `actions: read` - Read workflow information
- `contents: read` - Checkout repository
- `security-events: write` - Report security findings
- `GITHUB_TOKEN` - Create releases (automatic)

## Status Badges

The README displays real-time build status:

```markdown
[![CI/CD Pipeline](https://github.com/Reblayzer/BachelorCode/actions/workflows/ci.yml/badge.svg)](https://github.com/Reblayzer/BachelorCode/actions/workflows/ci.yml)
[![Security](https://github.com/Reblayzer/BachelorCode/actions/workflows/security.yml/badge.svg)](https://github.com/Reblayzer/BachelorCode/actions/workflows/security.yml)
```

## Local Testing

Before pushing code, test locally to catch issues early:

```bash
# Test backend build
dotnet restore StorageConnector.sln
dotnet build StorageConnector.sln --configuration Release
dotnet test StorageConnector.sln --configuration Release

# Test frontend build
cd web
npm ci
npm run lint
npm test -- --run
npm run build
```

## Viewing Results

### GitHub Actions Tab
Navigate to: `https://github.com/Reblayzer/BachelorCode/actions`

- See all workflow runs
- View detailed logs
- Download artifacts
- Re-run failed jobs

### Pull Requests
- Each PR shows automated check status
- Green checkmark = all checks passed
- Red X = failure (click for details)
- Yellow circle = in progress

### Releases
Navigate to: `https://github.com/Reblayzer/BachelorCode/releases`

- Download deployment-ready binaries
- View release notes
- See version history

## Troubleshooting

### Workflow Fails on NuGet Restore
**Cause:** Missing or misconfigured package sources

**Solution:**
- Check NuGet.config is committed
- Verify package sources are accessible
- Check for authentication issues

### Frontend Tests Fail in CI
**Cause:** Missing dependencies or environment differences

**Solution:**
- Use `npm ci` instead of `npm install` (ensures exact versions)
- Check for missing environment variables
- Review test output in Actions log

### Release Creation Fails
**Cause:** Missing GITHUB_TOKEN permissions

**Solution:**
- Ensure repository settings allow Actions to create releases
- Check workflow has correct permissions in YAML

### Security Scan Reports False Positives
**Cause:** Overly sensitive security rules

**Solution:**
- Review CodeQL findings manually
- Add exceptions for known safe patterns
- Document why certain warnings are acceptable

## Best Practices

### Branch Strategy
- `main` - Production-ready code
- `develop` - Integration branch
- Feature branches - New features/fixes

### Commit Messages
Use conventional commits for clarity:
```
feat: add user profile page
fix: resolve login redirect issue
docs: update API documentation
test: add integration tests for OAuth flow
```

### Testing Before Push
```bash
# Run all checks locally
dotnet test StorageConnector.sln
cd web && npm test -- --run
```

### Creating Releases
1. Merge all changes to `main`
2. Update version in documentation
3. Create and push tag:
   ```bash
   git tag v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```
4. Release workflow automatically creates GitHub Release

## Maintenance

### Updating Workflows
- Workflow files are in `.github/workflows/`
- Test changes in feature branch
- Verify with pull request checks

### Updating Dependencies
- Run security workflow manually after updating packages
- Review dependency audit results
- Update lockfiles (`package-lock.json`) when updating npm packages

### Monitoring
- Enable email notifications for workflow failures
- Review security scan results weekly
- Keep GitHub Actions versions updated

## Integration with Development Process

### Daily Development
1. Create feature branch
2. Make changes
3. Push to GitHub
4. CI runs automatically
5. Review results in PR
6. Merge when green

### Release Process
1. Complete all features for version
2. Merge to `main`
3. Tag version: `git tag v1.0.0`
4. Push tag: `git push origin v1.0.0`
5. Release created automatically
6. Download and deploy artifacts

### Security Review
1. Weekly automated scan runs
2. Review security findings
3. Update vulnerable dependencies
4. Document accepted risks

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CI/CD Best Practices](https://docs.microsoft.com/dotnet/devops/)
- [CodeQL Documentation](https://codeql.github.com/docs/)
