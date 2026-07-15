Imports System.Configuration
Imports System.Security.Claims
Imports System.Threading.Tasks
Imports Microsoft.IdentityModel.Protocols.OpenIdConnect
Imports Microsoft.Owin.Security
Imports Microsoft.Owin.Security.Cookies
Imports Microsoft.Owin.Security.OpenIdConnect
Imports Owin

''' <summary>
''' OWIN startup for the Web Forms app. Signs the user in with Entra ID (OpenID
''' Connect, authorization-code + id_token hybrid flow) against the configured
''' tenant, and redeems the auth code for a Fabric SQL access token ON BEHALF OF
''' the signed-in user via MSAL. That user token is what Fabric evaluates for
''' RLS / OneLake security, so each user sees only their rows.
'''
''' Registered via Web.config: &lt;add key="owin:AppStartup" value="Startup" /&gt;
''' </summary>
Public Class Startup
    Public Sub Configuration(app As IAppBuilder)
        Dim clientId = ConfigurationManager.AppSettings("ida:ClientId")
        Dim tenantId = ConfigurationManager.AppSettings("ida:TenantId")
        Dim redirectUri = ConfigurationManager.AppSettings("ida:RedirectUri")
        Dim sqlScope = ConfigurationManager.AppSettings("ida:SqlScope")
        Dim authority = String.Format("https://login.microsoftonline.com/{0}/v2.0", tenantId)

        app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType)
        app.UseCookieAuthentication(New CookieAuthenticationOptions())

        app.UseOpenIdConnectAuthentication(New OpenIdConnectAuthenticationOptions() With {
            .ClientId = clientId,
            .Authority = authority,
            .RedirectUri = redirectUri,
            .PostLogoutRedirectUri = redirectUri,
            .ResponseType = OpenIdConnectResponseType.CodeIdToken,
            .Scope = "openid profile offline_access " & sqlScope,
            .TokenValidationParameters = New Microsoft.IdentityModel.Tokens.TokenValidationParameters() With {
                .ValidateIssuer = False
            },
            .Notifications = New OpenIdConnectAuthenticationNotifications() With {
                .AuthorizationCodeReceived = Function(context) OnAuthorizationCodeReceived(context, sqlScope)
            }
        })
    End Sub

    ''' <summary>
    ''' Redeem the authorization code for a token for the Fabric SQL scope and
    ''' stamp the MSAL account id onto the identity so the page can silently
    ''' re-acquire the token later.
    ''' </summary>
    Private Async Function OnAuthorizationCodeReceived(
        context As Notifications.AuthorizationCodeReceivedNotification, sqlScope As String) As Task

        Dim cca = MsalAppBuilder.GetApp()
        Dim result = Await cca.AcquireTokenByAuthorizationCode({sqlScope}, context.Code).ExecuteAsync()

        ' Capture the Fabric access token now (in-process store keyed by account id)
        ' so the page can use it directly without relying on MSAL silent cache retrieval.
        Dim key As String = "user"
        If result.Account IsNot Nothing AndAlso result.Account.HomeAccountId IsNot Nothing Then
            key = result.Account.HomeAccountId.Identifier
        End If
        MsalAppBuilder.UserAccessTokens(key) = Tuple.Create(result.AccessToken, result.ExpiresOn)

        Dim identity = context.AuthenticationTicket.Identity
        identity.AddClaim(New Claim("msal_account", key))
    End Function
End Class
