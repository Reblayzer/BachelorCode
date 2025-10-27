# Microsoft OAuth Setup

1. Create an **App registration** in [Azure Portal](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).
   - Supported account types: pick the audience that matches your product. `Accounts in any organizational directory and personal Microsoft accounts` maps to the `common` tenant.
   - Redirect URI (web): `https://<api-host>/api/connect/microsoft/callback`
   - For local HTTPS development: `https://localhost:5001/api/connect/microsoft/callback`.
2. Under **Certificates & secrets**, create a new client secret and copy it somewhere safe.
3. Grant the read-only scopes used by StorageConnector:
   - API Permissions → Add permission → Microsoft Graph → Delegated.
   - Add `Files.Read` and `Sites.Read.All`.
   - Add `offline_access` (included automatically for Microsoft Graph when `Add` is clicked).
   - Click **Grant admin consent** if your directory requires it.
4. Store credentials securely for the API:
   ```bash
   dotnet user-secrets set "OAuth:Microsoft:TenantId" "common"               # or your tenant GUID
   dotnet user-secrets set "OAuth:Microsoft:ClientId" "your-client-id"
   dotnet user-secrets set "OAuth:Microsoft:ClientSecret" "your-client-secret"
   ```
   Alternatively, supply them as environment variables (`OAuth__Microsoft__ClientId`, etc.) in production.
5. Restart the API. The `/api/connect/microsoft/start` endpoint now issues real Microsoft authorization URLs.
