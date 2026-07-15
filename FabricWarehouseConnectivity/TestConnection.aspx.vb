Imports System.Configuration
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>
''' Code-behind for the connectivity test page. Clicking the button opens a
''' connection to the Fabric Warehouse, prints @@VERSION, and runs the optional
''' sample query from Web.config (appSettings key "Fabric:SampleQuery").
''' </summary>
Partial Class TestConnection
    Inherits System.Web.UI.Page

    Protected Sub btnTest_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        Try
            ' 1) Prove we can authenticate and reach the TDS endpoint.
            Dim version As String = FabricWarehouseConnector.TestConnection()
            litStatus.Text = "<span class='ok'>Connected successfully to the Fabric Warehouse.</span>"
            litVersion.Text = "<pre>" & Server.HtmlEncode(version) & "</pre>"

            ' 2) Optional sample query.
            Dim sampleQuery As String = ConfigurationManager.AppSettings("Fabric:SampleQuery")
            If Not String.IsNullOrWhiteSpace(sampleQuery) Then
                Dim data As DataTable = FabricWarehouseConnector.ExecuteQuery(sampleQuery)
                gvResults.DataSource = data
                gvResults.DataBind()
            End If

        Catch ex As SqlException
            ' SqlException here almost always means: SQL auth attempted, SPN not
            ' granted workspace access, or the tenant setting is disabled.
            litStatus.Text = "<span class='err'>SQL error connecting to Fabric Warehouse:" &
                             Environment.NewLine & Server.HtmlEncode(ex.Message) & "</span>"
        Catch ex As Exception
            litStatus.Text = "<span class='err'>Error: " &
                             Server.HtmlEncode(ex.Message) & "</span>"
        End Try
    End Sub

End Class
