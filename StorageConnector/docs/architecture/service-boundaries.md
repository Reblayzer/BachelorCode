# Service Boundary Sketch

This document outlines the initial microservice split for the StorageConnector MVP. It favours clear responsibilities, minimal duplication, and aligns with SOLID principles so each service has a single reason to change.

## Overview

| Service             | Responsibility                                                                 | Data store                         | External dependencies                       |
|---------------------|---------------------------------------------------------------------------------|------------------------------------|---------------------------------------------|
| **Identity Service**| Registration, login, email confirmation, issuing auth tokens/cookies           | Identity DB (ASP.NET Identity)     | SendGrid                                    |
| **Linking Service** | Manage third-party account links, OAuth flows, encrypted refresh tokens        | Linking DB (ProviderAccount table) | Google OAuth, Microsoft OAuth               |
| **Gateway / BFF**   | (Optional) Aggregates Identity + Linking APIs behind a single public surface    | None                               | Talks to Identity + Linking over HTTP       |

React SPA communicates with the public gateway (or directly with both services in development). Services communicate over REST/HTTP initially—simple and sufficient for the MVP. A message bus can be introduced later if sync or background jobs expand.

## Identity Service

- **Input**: `/api/auth/register`, `/login`, `/logout`, `/confirm-email`, `/resend-confirmation`.
- **Output**: issues secure cookies or JWT tokens used to call the Linking service.
- **Entities**: `ApplicationUser`, Identity roles, email confirmation tokens.
- **Rules**:
  - Users must confirm email before login (`SignInOptions` enforce this).
  - Password policy, lockout, etc., live here.
  - SendGrid integration encapsulated in this service.
- **Interactions**: exposes a minimal DTO contract (e.g., `RegisterRequest`, `LoginRequest`) to keep other services decoupled from Identity internals.

## Linking Service

- **Input**: `/api/connect/{provider}/start`, `/callback`, `/disconnect`, future `/connections` list.
- **Output**: stores provider-specific refresh tokens (encrypted) and exposes metadata to the SPA.
- **Entities**: `ProviderAccount`, OAuth state records (in cache), short-lived tokens in memory.
- **Rules**:
  - Uses PKCE and provider-specific scopes defined in configuration.
  - Validates calling user via Identity-issued cookie/JWT (forward the `userId` claim).
  - Encrypts refresh tokens at rest using Data Protection or per-service key vault.
- **Interactions**: Identity cookie/JWT gets forwarded via reverse proxy; no direct DB access to the Identity store.

## Cross-cutting Concerns

| Concern          | Approach                                                                 |
|------------------|--------------------------------------------------------------------------|
| Auth propagation | Identity sets an auth cookie (same-site) or JWT; gateway forwards it     |
| Configuration    | Each service has its own `appsettings.*` and secrets sets (user-secrets, key vault) |
| Logging/Tracing  | Structured logging with Serilog + shared correlation ID middleware       |
| Validation       | FluentValidation or data annotations, specific to each service’s DTOs    |
| Deployment       | Each service packaged as independent container; optional docker-compose for local dev |

## Development Flow

1. **Shared Contracts**: Extract DTOs/interfaces used by both services (e.g., `LoginRequest`) into a shared project/nuget to avoid duplication but keep dependencies narrow.
2. **Split the DbContext**: Identity service keeps `AppDbContext` for users; Linking service owns a slim context with `ProviderAccount`. Avoid sharing migrations.
3. **Reverse Proxy/Gateway**: During transition, keep the existing API as a façade that proxies requests to the new services. Gradually migrate endpoints.
4. **React SPA**: Points to gateway in production. Locally, Vite dev server can call the façade or services directly once CORS is enabled.

## Open Questions

- **Token shape**: Stick with cookies for user session or switch to JWT for service-to-service calls? (For MVP, cookies are ok if gateway and services share a domain.)
- **Re-auth**: When refresh token refresh fails (e.g., revoked), how does Linking service notify the SPA? Consider eventing or 401 responses with a descriptive payload.
- **Future services**: A file synchronization service will likely subscribe to provider changes. Plan eventual message contracts once scope is clearer.

## Next Steps

1. Align with supervisor on the boundary sketch.
2. Introduce a `StorageConnector.IdentityService` project and migrate existing auth endpoints there.
3. Carve the linking endpoints into `StorageConnector.LinkingService`, using the current code as a starting point.
4. Add a thin gateway (or keep the current API) to route requests while the React SPA is being developed.
