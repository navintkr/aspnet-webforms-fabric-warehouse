# ASP.NET Web Forms (VB.NET) to Microsoft Fabric Warehouse

A minimal ASP.NET **Web Forms (Visual Basic)** app on **.NET Framework 4.8** that connects to a
**Microsoft Fabric Warehouse** using **Microsoft.Data.SqlClient 7.0.2** and **Microsoft Entra ID**
authentication.

It demonstrates two patterns:

- **Service principal** (server identity) for a classic IIS-hosted app.
- **Per-user sign-in** (OWIN OpenID Connect + MSAL on-behalf-of) so Fabric applies
  **row-level security** for the signed-in user.

Replace every `<<placeholder>>` with your own values before running.

## What you need

- Visual Studio 2022 with the **ASP.NET and web development** workload (.NET Framework 4.8).
- A Fabric Warehouse and its SQL connection string (server host ending in
  `.datawarehouse.fabric.microsoft.com`).
- An Entra app registration (client id, tenant id, and a client secret).
- Outbound **TCP 1433** open (Fabric uses port 1433 only, no redirect ports).

> Fabric Warehouse supports **Microsoft Entra ID authentication only**. SQL logins
> (`User Id` / `Password`) are not supported. Every direct connection string must include an
> `Authentication=Active Directory ...` keyword.

## One-time Entra and Fabric setup

1. **App registration**: Entra ID > App registrations > New registration. Note the
   **Application (client) ID** and **Directory (tenant) ID**. Under **Certificates & secrets**,
   create a client secret and copy its value.
2. **Redirect URI**: add a **Web** redirect URI for both local and cloud, for example
   `https://localhost:44300/` and `https://<<your-webapp>>.azurewebsites.net/`. Enable **ID tokens**.
3. **API permission**: add delegated **Azure SQL Database / user_impersonation** and grant consent.
4. **Workspace access**: a workspace Admin adds the app registration (or the user) to the Fabric
   workspace with at least the **Viewer** role.
5. **Object permissions (optional)**: run `sql/grant-access.sql` against your warehouse to grant
   least-privilege access to the service principal.

## Configure

Edit `Web.config` and replace the placeholders:

| Placeholder | Where | Value |
|-------------|-------|-------|
| `<<your-warehouse>>` | `connectionStrings` (Server) | Warehouse SQL host (before `.datawarehouse...`) |
| `<<your-database>>` | `connectionStrings` (Database) | Warehouse (database) name |
| `<<your-client-id>>` | `connectionStrings` User Id and `ida:ClientId` | App registration client id |
| `<<your-tenant-id>>` | `ida:TenantId` | Directory (tenant) id |
| `<<your-webapp>>` | `ida:RedirectUri` | App Service name (cloud) |

Never commit the client secret. Provide it at runtime instead:

- **Service principal string** (`FabricWarehouse`): the code reads the secret from the
  `FABRIC_SP_SECRET` environment variable / App Service setting.
- **Per-user sign-in** (`ida:ClientSecret`): set it as an App Service application setting, not in the file.

## Run locally

1. Open the project in Visual Studio.
2. Right-click the project > **Manage NuGet Packages** > **Restore** (uses `packages.config`).
   Let Visual Studio keep `autoGenerateBindingRedirects` on so the binding redirects match the
   restored assemblies.
3. Set `Default.aspx` as the start page and press **F5** (runs under IIS Express).
4. Sign in with your Entra user when prompted, then click
   **Connect to Fabric and load Weather**. The grid binds
   `SELECT TOP 100 * FROM [dbo].[Weather]` using a token acquired for the signed-in user.

`TestConnection.aspx` is a minimal probe that runs `SELECT @@VERSION` plus the optional
`Fabric:SampleQuery` from `Web.config`.

## Deploy as an Azure Web App

1. Create the App Service (Windows, .NET Framework 4.8):

   ```powershell
   az group create -n <<your-rg>> -l <<your-region>>
   az appservice plan create -g <<your-rg>> -n <<your-plan>> --sku F1
   az webapp create -g <<your-rg>> -p <<your-plan>> -n <<your-webapp>> --runtime "ASPNET:V4.8"
   ```

2. Set the secrets as application settings (kept out of source control):

   ```powershell
   az webapp config appsettings set -g <<your-rg>> -n <<your-webapp>> --settings `
     ida:ClientSecret="<<your-client-secret>>" `
     FABRIC_SP_SECRET="<<your-client-secret>>"
   ```

3. Make sure the app registration has `https://<<your-webapp>>.azurewebsites.net/` as a **Web**
   redirect URI.

4. Publish from Visual Studio (**Build > Publish**), or deploy a zip of the built site:

   ```powershell
   Compress-Archive -Path .\* -DestinationPath site.zip -Force
   az webapp deploy -g <<your-rg>> -n <<your-webapp>> --src-path site.zip --type zip
   ```

5. Browse to `https://<<your-webapp>>.azurewebsites.net`, sign in, and load the grid.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Cannot find an authentication provider for 'ActiveDirectory...'` | Install `Microsoft.Data.SqlClient.Extensions.Azure` 7.0.2. The Entra providers moved out of the core package in v7.0. |
| `Login failed for user '<token-identified principal>'` | The identity is not granted workspace access, or the tenant setting is off. |
| `Login failed` with a SQL login | Add the `Authentication=Active Directory ...` keyword. SQL auth is not supported. |
| Timeout / cannot reach server | Outbound **TCP 1433** is blocked. Fabric does not use redirect ports. |
| `Could not load file or assembly ...` | Missing or wrong binding redirect. Keep `autoGenerateBindingRedirects` on and rebuild. |
| SSL / login error after connect | Ensure `Encrypt=True` and that the OS trusts current root CAs. |
| Works locally, fails on IIS | You used `Active Directory Interactive`. Use service principal or managed identity on the server. |

## Project layout

| Path | Purpose |
|------|---------|
| `Default.aspx(.vb)` | Home page: per-user sign-in and Weather grid |
| `TestConnection.aspx(.vb)` | Connectivity probe (`@@VERSION` + sample query) |
| `App_Code/FabricWarehouseConnector.vb` | Reusable data-access helper |
| `App_Code/Startup.vb` | OWIN OpenID Connect sign-in and code redemption |
| `App_Code/MsalAppBuilder.vb` | MSAL confidential client + per-user token store |
| `sql/grant-access.sql` | Optional T-SQL to grant the service principal access |
| `Web.config` | Connection strings, app settings, binding redirects |
| `packages.config` | NuGet references |

## References

- Entra authentication in Fabric: https://learn.microsoft.com/fabric/data-warehouse/entra-id-authentication
- Warehouse connectivity: https://learn.microsoft.com/fabric/data-warehouse/connectivity
- Service principals in Fabric: https://learn.microsoft.com/fabric/data-warehouse/service-principals
