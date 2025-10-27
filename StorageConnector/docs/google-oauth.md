# Google OAuth Setup

Follow these steps to enable Google account linking in StorageConnector.

1. Create an OAuth 2.0 **Web** client ID in [Google Cloud Console](https://console.cloud.google.com/apis/credentials).
   - Authorized redirect URI: `https://<gateway-host>/api/connect/google/callback`
   - For local development with the Linking service running on its default port, add `https://localhost:7030/api/connect/google/callback`.
2. Enable the **Google Drive API** on the same project so the read-only scope works.
3. Store the credentials securely:
   ```bash
   dotnet user-secrets set "OAuth:Google:ClientId" "your-client-id"
   dotnet user-secrets set "OAuth:Google:ClientSecret" "your-client-secret"
   ```
   Replace `dotnet user-secrets` with environment variables if you deploy the app (e.g. `OAuth__Google__ClientId`).
4. Confirm `appsettings.json` still contains the `OAuth:Google` section with `AccessType` = `offline` and `Prompt` = `consent`. These flags request a refresh token; you do **not** need the `offline_access` scope for Google.
5. Restart the API after updating secrets. The `/api/connect/google/start` endpoint will now emit a live Google authorization URL.
