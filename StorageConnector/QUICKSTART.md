# Quick Start Guide - Microservices Architecture

## 🚀 Running StorageConnector Microservices

### Prerequisites

- .NET 9.0 SDK installed
- Node.js 18+ installed
- PowerShell

### Option 1: With API Gateway (Recommended)

#### Step 1: Configure Secrets

Generate a 64-character JWT secret:

```powershell
$secret = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Write-Host $secret
```

Configure all three components with the **same** secret:

```powershell
# IdentityService
dotnet user-secrets set "Jwt:SecretKey" "$secret" --project services/IdentityService/Api/IdentityService.Api.csproj

# LinkingService
dotnet user-secrets set "Jwt:SecretKey" "$secret" --project services/LinkingService/Api/LinkingService.Api.csproj

# API Gateway
cd ApiGateway
dotnet user-secrets set "Jwt:SecretKey" "$secret"
dotnet user-secrets set "Jwt:Issuer" "StorageConnector.IdentityService"
dotnet user-secrets set "Jwt:Audience" "StorageConnector"
cd ..
```

Configure email (IdentityService):

```powershell
dotnet user-secrets set "EmailSettings:ApiKey" "YOUR_SENDGRID_KEY" --project services/IdentityService/Api/IdentityService.Api.csproj
```

Configure OAuth (LinkingService):

```powershell
dotnet user-secrets set "OAuth:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID" --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET" --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets set "OAuth:Microsoft:ClientId" "YOUR_MS_CLIENT_ID" --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets set "OAuth:Microsoft:ClientSecret" "YOUR_MS_CLIENT_SECRET" --project services/LinkingService/Api/LinkingService.Api.csproj
```

#### Step 2: Create Databases

```powershell
# IdentityService database
dotnet ef database update --project services/IdentityService/Api/IdentityService.Api.csproj

# LinkingService database
dotnet ef database update --project services/LinkingService/Api/LinkingService.Api.csproj
```

#### Step 3: Start All Services

Open 4 PowerShell terminals:

**Terminal 1 - IdentityService:**

```powershell
cd services\IdentityService\Api
dotnet run
```

Wait for: `Now listening on: https://localhost:7166`

**Terminal 2 - LinkingService:**

```powershell
cd services\LinkingService\Api
dotnet run
```

Wait for: `Now listening on: https://localhost:7134`

**Terminal 3 - API Gateway:**

```powershell
cd ApiGateway
dotnet run
```

Wait for: `Now listening on: https://localhost:5001`

**Terminal 4 - Frontend:**

```powershell
cd web
npm install  # First time only
npm run dev
```

Wait for: `Local: http://localhost:5173`

#### Step 4: Access the Application

Open your browser to: **http://localhost:5173**

### Option 2: Direct Service Access (Legacy)

If you want to skip the API Gateway for development:

1. Update `web/.env`:

```env
# Comment out gateway
# VITE_API_BASE_URL=https://localhost:5001

# Use direct access
VITE_IDENTITY_BASE_URL=https://localhost:7166
VITE_LINKING_BASE_URL=https://localhost:7134
```

2. Start only services + frontend (skip Terminal 3)

## ✅ Verify Everything Works

### 1. Check Health Endpoints

```powershell
# Gateway health
curl https://localhost:5001/health

# Identity health (through gateway)
curl https://localhost:5001/health/identity

# Linking health (through gateway)
curl https://localhost:5001/health/linking
```

Expected response:

```json
{
  "status": "Healthy"
}
```

### 2. Test User Registration

```powershell
curl -X POST https://localhost:5001/api/v1/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'
```

### 3. Test Login

```powershell
curl -X POST https://localhost:5001/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'
```

Expected response:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "test@example.com"
}
```

### 4. Test Authenticated Endpoint

```powershell
$token = "YOUR_TOKEN_FROM_LOGIN"
curl https://localhost:5001/api/v1/auth/me `
  -H "Authorization: Bearer $token"
```

## 🔍 Monitoring

### View Logs

Each service outputs logs to console. Watch for:

- `X-Correlation-ID` in logs (request tracking)
- HTTP status codes
- Error messages

### Check Correlation IDs

Send request with custom correlation ID:

```powershell
curl https://localhost:5001/api/v1/auth/me `
  -H "X-Correlation-ID: test-123" `
  -H "Authorization: Bearer $token"
```

Search logs across all terminals for `test-123` to trace the request.

## 🛑 Stopping Services

Press `Ctrl+C` in each terminal to stop services gracefully.

## 🐛 Troubleshooting

### "Port already in use"

```powershell
# Find process using the port
netstat -ano | findstr :7166
netstat -ano | findstr :7134
netstat -ano | findstr :5001

# Kill the process (replace PID)
taskkill /PID <PID> /F
```

### "Database not found"

```powershell
# Re-run migrations
dotnet ef database update --project services/IdentityService/Api/IdentityService.Api.csproj
dotnet ef database update --project services/LinkingService/Api/LinkingService.Api.csproj
```

### "JWT secret not configured"

```powershell
# Verify secrets are set
dotnet user-secrets list --project services/IdentityService/Api/IdentityService.Api.csproj
dotnet user-secrets list --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets list --project ApiGateway/ApiGateway.csproj
```

### "CORS error in browser"

Check that `web/.env` has the correct API URL and services are running.

### "Service Unavailable (503)"

- Verify IdentityService and LinkingService are running
- Check health endpoints directly:
  ```powershell
  curl https://localhost:7166/health
  curl https://localhost:7134/health
  ```

## 📊 API Endpoints Reference

### Through API Gateway (Port 5001)

| Path                     | Service  | Description        |
| ------------------------ | -------- | ------------------ |
| `/api/v1/auth/**`        | Identity | Authentication     |
| `/api/v1/connect/**`     | Linking  | OAuth connections  |
| `/api/v1/connections/**` | Linking  | Manage connections |
| `/api/v1/files/**`       | Linking  | File operations    |
| `/health`                | Gateway  | Gateway health     |
| `/health/identity`       | Identity | Identity health    |
| `/health/linking`        | Linking  | Linking health     |

### Direct Access (Development)

| Service         | Port | Base URL                 |
| --------------- | ---- | ------------------------ |
| IdentityService | 7166 | `https://localhost:7166` |
| LinkingService  | 7134 | `https://localhost:7134` |
| Frontend        | 5173 | `http://localhost:5173`  |

## 🎯 Next Steps

1. **Register a user** via the frontend
2. **Confirm email** (check console for confirmation link)
3. **Login** to get a JWT token
4. **Connect Google Drive** or **OneDrive**
5. **Browse files** from connected providers

## 📚 More Information

- **Architecture Analysis**: See `MICROSERVICES_ANALYSIS.md`
- **Deployment Guide**: See `DEPLOYMENT_GUIDE.md`
- **Implementation Details**: See `IMPLEMENTATION_SUMMARY.md`
- **Security Setup**: See `SECURITY_SETUP.md`

## 🎉 Success Indicators

You'll know everything is working when:

- ✅ All 4 terminals show services running
- ✅ Health checks return "Healthy"
- ✅ Frontend loads at http://localhost:5173
- ✅ You can register and login
- ✅ Correlation IDs appear in logs

---

**Happy coding! 🚀**
