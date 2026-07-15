Imports System.Configuration
Imports System.Data
Imports System.Security.Claims
Imports Microsoft.Owin.Security
Imports Microsoft.Owin.Security.OpenIdConnect

''' <summary>
''' Home page. Requires the user to sign in with Entra ID (OWIN OpenID Connect).
''' Once signed in, it silently acquires a Fabric SQL token for THAT user and
''' loads [dbo].[Weather], so Fabric row-level security applies per user.
''' </summary>
Partial Class _Default
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Request.IsAuthenticated Then
            ' Kick off interactive sign-in against Entra ID.
            HttpContext.Current.GetOwinContext().Authentication.Challenge(
                New AuthenticationProperties With {.RedirectUri = "/"},
                OpenIdConnectAuthenticationDefaults.AuthenticationType)
            Return
        End If

        If Not IsPostBack Then
            LoadWeather()
        End If
    End Sub

    Protected Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        LoadWeather()
    End Sub

    Private Sub LoadWeather()
        Try
            Dim token As String = GetFabricTokenForUser()
            Dim data As DataTable = FabricWarehouseConnector.GetWeatherWithToken(token, 100)
            gvWeather.DataSource = data
            gvWeather.DataBind()
            litStatus.Text = "<span class='ok'>Connected to the Fabric Warehouse as " &
                             Server.HtmlEncode(User.Identity.Name) & ". Loaded " & data.Rows.Count &
                             " rows from dbo.Weather (row-level security applied for this identity).</span>"
        Catch ex As Exception
            Dim sb As New System.Text.StringBuilder()
            Dim e As Exception = ex
            Do While e IsNot Nothing
                sb.AppendLine(e.GetType().FullName & ": " & e.Message)
                If TypeOf e Is System.Reflection.ReflectionTypeLoadException Then
                    For Each le In DirectCast(e, System.Reflection.ReflectionTypeLoadException).LoaderExceptions
                        sb.AppendLine("  LoaderException: " & le.Message)
                    Next
                End If
                e = e.InnerException
            Loop
            sb.AppendLine()
            sb.AppendLine(ex.StackTrace)
            litStatus.Text = "<pre class='err'>" & Server.HtmlEncode(sb.ToString()) & "</pre>"
        End Try
    End Sub

    ''' <summary>
    ''' Silently acquires a Fabric SQL access token for the signed-in user using the
    ''' MSAL account captured during sign-in.
    ''' </summary>
    Private Function GetFabricTokenForUser() As String
        Dim principal = TryCast(User, ClaimsPrincipal)
        Dim accountId As String = Nothing
        If principal IsNot Nothing Then
            Dim claim = principal.FindFirst("msal_account")
            If claim IsNot Nothing Then accountId = claim.Value
        End If

        ' Fast path: token captured at sign-in (in-process store).
        If accountId IsNot Nothing Then
            Dim entry As Tuple(Of String, DateTimeOffset) = Nothing
            If MsalAppBuilder.UserAccessTokens.TryGetValue(accountId, entry) Then
                If entry.Item2 > DateTimeOffset.UtcNow.AddMinutes(2) Then
                    Return entry.Item1
                End If
            End If
        End If

        ' Fallback: MSAL silent (if the token cache still has the account).
        Dim scopes = {ConfigurationManager.AppSettings("ida:SqlScope")}
        Dim app = MsalAppBuilder.GetApp()
        If accountId IsNot Nothing Then
            Dim account = app.GetAccountAsync(accountId).GetAwaiter().GetResult()
            If account IsNot Nothing Then
                Dim result = app.AcquireTokenSilent(scopes, account).ExecuteAsync().GetAwaiter().GetResult()
                Return result.AccessToken
            End If
        End If

        Throw New Exception("Fabric token is not available for this session. Please sign out (/MicrosoftIdentity/Account/SignOut) and sign in again.")
    End Function

End Class
