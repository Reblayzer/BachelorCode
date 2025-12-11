# StorageConnector

[![CI/CD Pipeline](https://github.com/Reblayzer/BachelorCode/actions/workflows/ci.yml/badge.svg)](https://github.com/Reblayzer/BachelorCode/actions/workflows/ci.yml)
[![Security](https://github.com/Reblayzer/BachelorCode/actions/workflows/security.yml/badge.svg)](https://github.com/Reblayzer/BachelorCode/actions/workflows/security.yml)

A secure **microservices-based** application that enables users to connect and manage their cloud storage accounts (Google Drive, OneDrive) through a unified interface with true service isolation and production-ready patterns.

## 🏗️ Microservices Architecture

StorageConnector implements **true microservices** with Clean Architecture principles:

```
StorageConnector/
├── ApiGateway/                    # YARP reverse proxy (Port 5001)
├── services/
│   ├── IdentityService/          # Authentication & user management
│   │   ├── IdentityService.sln  # ← Independent solution
│   │   ├── Domain/               # Business entities
│   │   ├── Application/          # Use cases
│   │   ├── Infrastructure/       # Data access, email
│   │   └── Api/                  # REST API (Port 7166)
│   └── LinkingService/           # Cloud storage integration
│       ├── LinkingService.sln   # ← Independent solution
│       ├── Domain/               # Business entities
│       ├── Application/          # Use cases
│       ├── Infrastructure/       # OAuth, file providers
│       └── Api/                  # REST API (Port 7134)
├── tests/
│   ├── IdentityService.Tests/
│   └── LinkingService.Tests/
└── web/                          # React frontend (Vite + TypeScript)
```

### Key Architectural Improvements

✅ **Separate Solutions**: Each service independently buildable/deployable  
✅ **Token Introspection**: Secure inter-service authentication  
✅ **API Versioning**: `/api/v1/auth/...` with header support  
✅ **Health Checks**: `/health`, `/health/ready`, `/health/live`  
✅ **API Gateway**: YARP with circuit breaker & retry policies  
✅ **Correlation IDs**: Request tracing across services  
✅ **Independent Databases**: `identity.db` & `linking.db`  
✅ **Resilience Patterns**: Circuit breaker, retries, timeouts

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) (Windows) or compatible shell

### 1. Clone the Repository

```bash
git clone https://github.com/Reblayzer/BachelorCode.git
cd BachelorCode/StorageConnector
```

### 2. Configure Secrets

**IMPORTANT**: The application uses .NET User Secrets to store sensitive configuration. You must configure these before running the application.

#### Generate a JWT Secret Key

Run this PowerShell command to generate a secure 64-character key:

```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

Copy the output and use it in the following steps.

#### Configure IdentityService Secrets

```bash
# Set JWT secret (use the key generated above)
dotnet user-secrets set "Jwt:SecretKey" "YOUR_64_CHAR_KEY" --project services/IdentityService/Api/IdentityService.Api.csproj

# Set SendGrid API key (get from https://sendgrid.com)
dotnet user-secrets set "EmailSettings:ApiKey" "YOUR_SENDGRID_API_KEY" --project services/IdentityService/Api/IdentityService.Api.csproj
```

#### Configure LinkingService Secrets

```bash
# Set JWT secret (MUST be the same key as IdentityService)
dotnet user-secrets set "Jwt:SecretKey" "YOUR_64_CHAR_KEY" --project services/LinkingService/Api/LinkingService.Api.csproj

# Set Google OAuth credentials (get from https://console.cloud.google.com)
dotnet user-secrets set "OAuth:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID" --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET" --project services/LinkingService/Api/LinkingService.Api.csproj

# Set Microsoft OAuth credentials (get from https://portal.azure.com)
dotnet user-secrets set "OAuth:Microsoft:ClientId" "YOUR_MICROSOFT_CLIENT_ID" --project services/LinkingService/Api/LinkingService.Api.csproj
dotnet user-secrets set "OAuth:Microsoft:ClientSecret" "YOUR_MICROSOFT_CLIENT_SECRET" --project services/LinkingService/Api/LinkingService.Api.csproj
```

> For detailed setup instructions, see [SECURITY_SETUP.md](./SECURITY_SETUP.md)

### 3. Apply Database Migrations

```bash
# Create IdentityService database
dotnet ef database update --project services/IdentityService/Api/IdentityService.Api.csproj

# Create LinkingService database
dotnet ef database update --project services/LinkingService/Api/LinkingService.Api.csproj
```

This will create `identity.db` and `linking.db` SQLite databases in the root directory.

### 4. Install Frontend Dependencies

```bash
cd web
npm install
```

### 5. Run the Application

#### Option A: With API Gateway (Recommended)

The API Gateway provides a unified entry point with resilience patterns:

```bash
# Terminal 1: IdentityService
cd services/IdentityService/Api
dotnet run

# Terminal 2: LinkingService
cd services/LinkingService/Api
dotnet run

# Terminal 3: API Gateway
cd ApiGateway
dotnet user-secrets set "Jwt:SecretKey" "YOUR_64_CHAR_KEY"
dotnet run

# Terminal 4: Frontend
cd web
npm run dev
```

**Access the application:**

- Frontend: http://localhost:5173
- API Gateway: https://localhost:5001
- IdentityService (direct): https://localhost:7166
- LinkingService (direct): https://localhost:7134

#### Option B: Direct Service Access (Development)

From the `web` directory, run:

```bash
npm run dev
```

This will start all services and the frontend automatically.

## 📚 Architecture Documentation

- **[MICROSERVICES_ANALYSIS.md](./MICROSERVICES_ANALYSIS.md)** - Detailed analysis of architecture decisions
- **[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)** - Production deployment strategies
- **[SECURITY_SETUP.md](./SECURITY_SETUP.md)** - Security configuration guide

## Testing

### Run All Tests

```bash
# From the root directory
dotnet test tests/tests.csproj
```

### Run Frontend Tests

```bash
cd web

# Run tests
npm test

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

## Security Features

StorageConnector implements comprehensive security measures:

- **JWT Authentication** - Secure token-based authentication
- **Rate Limiting** - IP-based rate limiting to prevent brute force attacks
  - Login: 5 attempts per minute
  - Registration: 10 attempts per hour
  - General API: 100 requests per minute
- **Security Headers** - Protection against XSS, clickjacking, and MIME sniffing
- **HSTS** - HTTP Strict Transport Security in production
- **User Secrets** - Sensitive data stored outside source control
- **CORS** - Restricted cross-origin requests
- **Data Protection** - Encrypted OAuth tokens at rest
- **Email Confirmation** - Required for account activation

## API Documentation

### Microservices Endpoints

All services support **API versioning** via URL or headers.

#### IdentityService Endpoints

**Base URL (Direct)**: `https://localhost:7166/api/v1`  
**Base URL (Gateway)**: `https://localhost:5001/api/v1`

| Method | Endpoint                | Description               | Auth Required |
| ------ | ----------------------- | ------------------------- | ------------- |
| POST   | `/auth/register`        | Register new user         | No            |
| POST   | `/auth/login`           | Login and get JWT         | No            |
| GET    | `/auth/confirm-email`   | Confirm email address     | No            |
| POST   | `/auth/forgot-password` | Request password reset    | No            |
| POST   | `/auth/reset-password`  | Reset password with token | No            |
| GET    | `/auth/me`              | Get current user info     | Yes           |
| POST   | `/auth/introspect`      | Validate JWT token        | No            |

#### LinkingService Endpoints

**Base URL (Direct)**: `https://localhost:7134/api/v1`  
**Base URL (Gateway)**: `https://localhost:5001/api/v1`

| Method | Endpoint                              | Description              | Auth Required |
| ------ | ------------------------------------- | ------------------------ | ------------- |
| GET    | `/connect/{provider}/start`           | Start OAuth flow         | Yes           |
| GET    | `/connect/{provider}/callback`        | OAuth callback           | No            |
| GET    | `/connections/status`                 | List user's connections  | Yes           |
| POST   | `/connect/{provider}/disconnect`      | Disconnect provider      | Yes           |
| GET    | `/files/{provider}`                   | List files from provider | Yes           |
| GET    | `/files/{provider}/{fileId}/view-url` | Get file URL             | Yes           |

**Supported Providers**: `google`, `microsoft`

#### Health Check Endpoints

| Service         | Endpoint           | Description                           |
| --------------- | ------------------ | ------------------------------------- |
| API Gateway     | `/health`          | Gateway health                        |
| IdentityService | `/health/identity` | Identity service health (via gateway) |
| LinkingService  | `/health/linking`  | Linking service health (via gateway)  |
| All Services    | `/health/ready`    | Readiness probe                       |
| All Services    | `/health/live`     | Liveness probe                        |

## 🛠️ Development

### Project Structure

Each microservice follows **Clean Architecture** with four distinct layers:

- **Domain Layer** (`services/*/Domain`) - Business entities, value objects, and domain logic
- **Application Layer** (`services/*/Application`) - Use cases, interfaces, and business rules
- **Infrastructure Layer** (`services/*/Infrastructure`) - External concerns (database, email, OAuth, file providers)
- **API Layer** (`services/*/Api`) - REST API controllers, middleware, and configuration

This separation ensures:

- **Independence**: Each service is self-contained with its own layers
- **Testability**: Domain and application logic can be tested without infrastructure
- **Maintainability**: Clear boundaries between business logic and technical details

### Technology Stack

**Backend**

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core (SQLite)
- ASP.NET Identity
- SendGrid (email)
- AspNetCoreRateLimit

**Frontend**

- React 19
- TypeScript
- Vite
- TanStack Query
- React Router
- Tailwind CSS
- Zustand (state management)

### Building for Production

```bash
# Build backend services
dotnet build StorageConnector.sln --configuration Release

# Build frontend
cd web
npm run build
```

The frontend build output will be in `web/dist/`.

## Configuration

### Environment Variables

The application uses `appsettings.json` for configuration. Key settings:

**IdentityService**

- `ConnectionStrings:Default` - SQLite database path
- `Jwt:*` - JWT configuration (issuer, audience, expiration)
- `Email:SendGrid:*` - Email sender configuration
- `Cors:AllowedOrigins` - Allowed frontend origins

**LinkingService**

- `ConnectionStrings:Default` - SQLite database path
- `Frontend:BaseUrl` - Frontend URL for OAuth redirects
- `Cors:AllowedOrigins` - Allowed frontend origins

### User Secrets

Sensitive values are stored in .NET User Secrets:

- JWT secret keys
- SendGrid API key
- OAuth client IDs and secrets

See [SECURITY_SETUP.md](./SECURITY_SETUP.md) for details.

## Troubleshooting

### "No such table: Users" error

Run database migrations:

```bash
dotnet ef database update --project services/IdentityService/Api/IdentityService.Api.csproj
```

### "The specified key was too short" error

Ensure your JWT secret is at least 64 characters. Generate a new one using the PowerShell command in step 2.

### OAuth redirect not working

Verify that:

1. OAuth redirect URIs in Google/Microsoft console match: `https://localhost:7134/api/connect/{provider}/callback`
2. Frontend base URL is correctly configured in LinkingService `appsettings.json`

### Email confirmation not sending

1. Verify SendGrid API key is set in user secrets
2. Check SendGrid account is verified and not in sandbox mode
3. Verify sender email is added to SendGrid sender authentication

## CI/CD Pipeline

This project uses **GitHub Actions** for automated continuous integration and deployment.

### Automated Workflows

#### 1. **CI/CD Pipeline** (`.github/workflows/ci.yml`)

Runs on every push and pull request to `main` and `develop` branches:

- **Backend Build & Test**

  - Restores NuGet dependencies
  - Builds entire solution in Release mode
  - Runs all unit and integration tests
  - Publishes both microservices
  - Uploads test results and build artifacts

- **Frontend Build & Test**

  - Installs npm dependencies
  - Runs linter
  - Executes frontend tests
  - Builds production-ready bundle
  - Uploads frontend artifact

- **Code Quality**
  - Checks code formatting
  - Runs security scans

#### 2. **Release Workflow** (`.github/workflows/release.yml`)

Automatically creates releases when you push version tags:

```bash
# Create a release
git tag v1.0.0
git push origin v1.0.0
```

This will:

- Build all services in Release mode
- Create deployment-ready archives
- Generate GitHub Release with download links
- Attach service binaries as release assets

#### 3. **Security Scanning** (`.github/workflows/security.yml`)

Runs weekly (every Monday) and can be triggered manually:

- Checks for outdated dependencies
- Scans for vulnerable packages
- Runs CodeQL security analysis
- Reports potential security issues

### Viewing Build Status

- **Build Status**: Check the badges at the top of this README
- **Actions Tab**: Visit [GitHub Actions](https://github.com/Reblayzer/BachelorCode/actions) for detailed logs
- **Pull Requests**: See automated checks on every PR

### Running CI/CD Locally

To test the build process locally before pushing:

```bash
# Backend
dotnet restore StorageConnector.sln
dotnet build StorageConnector.sln --configuration Release
dotnet test StorageConnector.sln --configuration Release

# Frontend
cd web
npm ci
npm test -- --run
npm run build
```

## License

This project is developed as part of a Bachelor's thesis.

## Contributing

This is an academic project. For questions or issues, please contact the project maintainer.

## Related Documentation

- [SECURITY_SETUP.md](./SECURITY_SETUP.md) - Detailed security configuration guide
- [CI_CD.md](./CI_CD.md) - Complete CI/CD pipeline documentation
- [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Entity Framework Core Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
