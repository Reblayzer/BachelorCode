# Security Configuration Guide

## Required Setup Before Running

This project uses **User Secrets** to store sensitive configuration. You MUST configure secrets before the application will run.

## Quick Start

### 1. Generate a Strong JWT Secret Key

Run this PowerShell command to generate a secure 64-character random key:

```powershell
-join ((65..90) + (97..122) + (48..57) + (33, 35, 36, 37, 38, 42, 43, 45, 61, 63, 64, 95) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

Save the output - you'll need it for both services.

### 2. Configure IdentityService Secrets

Navigate to the IdentityService API directory and run:

```bash
cd services/IdentityService/Api

# Set JWT Secret (use the key you generated above)
dotnet user-secrets set "Jwt:SecretKey" "YOUR-64-CHAR-KEY-HERE"

# Set SendGrid API Key (get from https://app.sendgrid.com/settings/api_keys)
dotnet user-secrets set "Email:SendGrid:ApiKey" "YOUR-SENDGRID-API-KEY"
```

### 3. Configure LinkingService Secrets

Navigate to the LinkingService API directory and run:

```bash
cd services/LinkingService/Api

# Set JWT Secret (use the SAME key as IdentityService)
dotnet user-secrets set "Jwt:SecretKey" "YOUR-64-CHAR-KEY-HERE"

# Set Google OAuth Credentials (get from https://console.cloud.google.com/apis/credentials)
dotnet user-secrets set "OAuth:Google:ClientId" "YOUR-GOOGLE-CLIENT-ID"
dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR-GOOGLE-CLIENT-SECRET"

# Set Microsoft OAuth Credentials (get from https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
dotnet user-secrets set "OAuth:Microsoft:ClientId" "YOUR-MICROSOFT-CLIENT-ID"
dotnet user-secrets set "OAuth:Microsoft:ClientSecret" "YOUR-MICROSOFT-CLIENT-SECRET"
```

## Verify Configuration

List all configured secrets:

```bash
# In IdentityService/Api
dotnet user-secrets list

# In LinkingService/Api
dotnet user-secrets list
```

## Security Features

This application includes:

**Rate Limiting**

- Login: 5 attempts per minute
- Registration: 10 per hour
- General endpoints: 100 requests per minute

**Security Headers**

- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Content-Security-Policy
- Referrer-Policy
- Permissions-Policy

**HTTPS Enforcement**

- HSTS enabled in production
- HTTPS redirection

**Data Protection**

- OAuth tokens encrypted at rest
- Password hashing with BCrypt
- JWT token validation

**CORS Protection**

- Restricted to localhost origins in development

## Production Deployment

For production:

1. **Use Azure Key Vault** or similar secret management
2. **Change all default secrets** including JWT keys
3. **Enable HSTS** (already configured for non-development)
4. **Update CORS origins** to your production domain
5. **Review rate limiting** rules for your traffic patterns
6. **Enable logging** for security events
7. **Set up monitoring** for failed login attempts

## Troubleshooting

### "SendGrid ApiKey must be configured"

Run: `dotnet user-secrets set "Email:SendGrid:ApiKey" "YOUR-API-KEY"`

### "JWT validation failed"

Ensure the `Jwt:SecretKey` is THE SAME in both IdentityService and LinkingService.

### "OAuth authentication failed"

Verify your OAuth credentials are correct and the redirect URIs are configured in Google/Microsoft consoles.

## Where Secrets Are Stored

User secrets are stored in:

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

They are **never** committed to source control.
