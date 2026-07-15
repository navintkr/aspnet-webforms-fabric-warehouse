Imports System.Configuration
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>
''' Reusable data-access helper for connecting an ASP.NET Web Forms (VB.NET,
''' .NET Framework 4.8) application to a Microsoft Fabric Warehouse using
''' Microsoft.Data.SqlClient 7.0.2 and Microsoft Entra ID authentication.
'''
''' Fabric Warehouse supports Entra ID authentication ONLY. The connection string
''' (see Web.config) must include an "Authentication=Active Directory ..." keyword.
''' </summary>
Public Class FabricWarehouseConnector

    ''' <summary>
    ''' Builds the connection string. By default it reads the "FabricWarehouse"
    ''' entry from Web.config. If the environment variable FABRIC_SP_SECRET is set,
    ''' the secret is injected at runtime instead of being read from the file
    ''' (recommended so the client secret is not stored in source control).
    ''' </summary>
    Private Shared Function GetConnectionString() As String
        Return GetConnectionString("FabricWarehouse")
    End Function

    ''' <summary>
    ''' Builds the connection string for a specific named entry in Web.config.
    ''' If the environment variable FABRIC_SP_SECRET is set, the secret is injected
    ''' at runtime (recommended so the client secret is not stored in source control).
    ''' </summary>
    Private Shared Function GetConnectionString(name As String) As String
        Dim cfg = ConfigurationManager.ConnectionStrings(name)
        If cfg Is Nothing OrElse String.IsNullOrWhiteSpace(cfg.ConnectionString) Then
            Throw New ConfigurationErrorsException(
                "Connection string '" & name & "' is missing from Web.config.")
        End If

        Dim builder As New SqlConnectionStringBuilder(cfg.ConnectionString)

        ' Optional: pull the service principal secret from an environment variable
        ' (or wire this up to Azure Key Vault) rather than storing it in Web.config.
        Dim secret As String = Environment.GetEnvironmentVariable("FABRIC_SP_SECRET")
        If Not String.IsNullOrEmpty(secret) Then
            builder.Password = secret
        End If

        Return builder.ConnectionString
    End Function

    ''' <summary>
    ''' Opens a connection to the Fabric Warehouse using the named connection string.
    ''' Caller is responsible for disposing (use in a Using block).
    ''' </summary>
    Public Shared Function OpenConnection(Optional name As String = "FabricWarehouse") As SqlConnection
        Dim conn As New SqlConnection(GetConnectionString(name))
        conn.Open()
        Return conn
    End Function

    ''' <summary>
    ''' Local-dev helper: returns the top N rows of [dbo].[Weather] using the
    ''' interactive "FabricWarehouseInteractive" connection (pops an MFA browser prompt).
    ''' </summary>
    Public Shared Function GetWeather(Optional topN As Integer = 100) As DataTable
        Dim table As New DataTable()
        Using conn = OpenConnection("FabricWarehouseInteractive")
            Using cmd = New SqlCommand("SELECT TOP (@n) * FROM [dbo].[Weather] ORDER BY DateID DESC;", conn)
                cmd.CommandTimeout = 60
                cmd.Parameters.AddWithValue("@n", topN)
                Using reader = cmd.ExecuteReader()
                    table.Load(reader)
                End Using
            End Using
        End Using
        Return table
    End Function

    ''' <summary>
    ''' Per-user helper: returns the top N rows of [dbo].[Weather] using a Microsoft
    ''' Entra access token acquired for the SIGNED-IN USER (on-behalf-of). Fabric
    ''' applies row-level security / OneLake security for that user's identity, with
    ''' no shared service principal. The token is passed via SqlConnection.AccessToken.
    ''' The server/database come from the "FabricWarehouseToken" connection string.
    ''' </summary>
    Public Shared Function GetWeatherWithToken(accessToken As String, Optional topN As Integer = 100) As DataTable
        Dim table As New DataTable()
        Dim cfg = ConfigurationManager.ConnectionStrings("FabricWarehouseToken")
        If cfg Is Nothing OrElse String.IsNullOrWhiteSpace(cfg.ConnectionString) Then
            Throw New ConfigurationErrorsException(
                "Connection string 'FabricWarehouseToken' is missing from Web.config.")
        End If
        Using conn As New SqlConnection(cfg.ConnectionString)
            conn.AccessToken = accessToken
            conn.Open()
            Using cmd = New SqlCommand("SELECT TOP (@n) * FROM [dbo].[Weather] ORDER BY DateID DESC;", conn)
                cmd.CommandTimeout = 60
                cmd.Parameters.AddWithValue("@n", topN)
                Using reader = cmd.ExecuteReader()
                    table.Load(reader)
                End Using
            End Using
        End Using
        Return table
    End Function

    ''' <summary>
    ''' Quick connectivity test. Returns the warehouse @@VERSION banner.
    ''' </summary>
    Public Shared Function TestConnection() As String
        Using conn = OpenConnection()
            Using cmd = New SqlCommand("SELECT @@VERSION;", conn)
                cmd.CommandTimeout = 60
                Return Convert.ToString(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Runs a parameterized query and returns the results as a DataTable.
    ''' Always pass user input via <paramref name="parameters"/> - never
    ''' concatenate values into the SQL text (prevents SQL injection).
    ''' </summary>
    ''' <param name="sql">Parameterized T-SQL, for example "SELECT * FROM dbo.Sales WHERE Year = @year".</param>
    ''' <param name="parameters">Optional named parameters to bind.</param>
    Public Shared Function ExecuteQuery(sql As String,
                                        Optional parameters As IDictionary(Of String, Object) = Nothing) As DataTable
        Dim table As New DataTable()
        Using conn = OpenConnection()
            Using cmd = New SqlCommand(sql, conn)
                cmd.CommandTimeout = 60
                If parameters IsNot Nothing Then
                    For Each kvp In parameters
                        cmd.Parameters.AddWithValue(kvp.Key, If(kvp.Value, DBNull.Value))
                    Next
                End If
                Using reader = cmd.ExecuteReader()
                    table.Load(reader)
                End Using
            End Using
        End Using
        Return table
    End Function

    ''' <summary>
    ''' Executes a non-query (INSERT/UPDATE/DELETE) and returns rows affected.
    ''' Requires the SPN to have write permission (Member/Contributor role or a
    ''' T-SQL GRANT). Uses parameters to prevent SQL injection.
    ''' </summary>
    Public Shared Function ExecuteNonQuery(sql As String,
                                           Optional parameters As IDictionary(Of String, Object) = Nothing) As Integer
        Using conn = OpenConnection()
            Using cmd = New SqlCommand(sql, conn)
                cmd.CommandTimeout = 60
                If parameters IsNot Nothing Then
                    For Each kvp In parameters
                        cmd.Parameters.AddWithValue(kvp.Key, If(kvp.Value, DBNull.Value))
                    Next
                End If
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

End Class
