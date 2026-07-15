-- ============================================================================
-- Grant the service principal (Entra app) access inside your Fabric Warehouse.
--
-- Run this in the Fabric Warehouse query editor (or any Entra-authenticated
-- SQL client) against your warehouse database.
--
-- Prerequisite: the SPN has already been added to the workspace via
-- "Manage access" (that creates the login/mapping). These statements then
-- shape object-level permissions.
--
-- Replace <APP_DISPLAY_NAME> with the Entra app registration's display name.
-- ============================================================================

-- Create a database user mapped to the service principal (if not already present).
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'<APP_DISPLAY_NAME>')
BEGIN
    CREATE USER [<APP_DISPLAY_NAME>] FROM EXTERNAL PROVIDER;
END
GO

-- Read-only access to all current and future objects:
ALTER ROLE db_datareader ADD MEMBER [<APP_DISPLAY_NAME>];
GO

-- If the app also needs to write data, add:
-- ALTER ROLE db_datawriter ADD MEMBER [<APP_DISPLAY_NAME>];
-- GO

-- Or grant precise, least-privilege object-level permissions instead of role
-- membership, for example:
-- GRANT SELECT ON SCHEMA::dbo TO [<APP_DISPLAY_NAME>];
-- GRANT SELECT ON OBJECT::dbo.YourTable TO [<APP_DISPLAY_NAME>];
-- GO
