Imports System.Collections.Concurrent
Imports System.Configuration
Imports Microsoft.Identity.Client

''' <summary>
''' Builds and caches a single MSAL confidential-client application for the app
''' instance. Its in-memory token cache lets the page silently re-acquire the
''' Fabric SQL token for the signed-in user after the initial code redemption.
''' </summary>
Public Module MsalAppBuilder
    Private _app As IConfidentialClientApplication
    Private ReadOnly _sync As New Object()

    ''' <summary>
    ''' In-process store of the Fabric access token per signed-in user, captured at
    ''' sign-in (keyed by MSAL home account id). Item1 = access token, Item2 = expiry.
    ''' Fine for a single-instance demo app; use a distributed cache for scale-out.
    ''' </summary>
    Public ReadOnly UserAccessTokens As New ConcurrentDictionary(Of String, Tuple(Of String, DateTimeOffset))()

    Public Function GetApp() As IConfidentialClientApplication
        If _app Is Nothing Then
            SyncLock _sync
                If _app Is Nothing Then
                    Dim clientId = ConfigurationManager.AppSettings("ida:ClientId")
                    Dim tenantId = ConfigurationManager.AppSettings("ida:TenantId")
                    Dim clientSecret = ConfigurationManager.AppSettings("ida:ClientSecret")
                    Dim redirectUri = ConfigurationManager.AppSettings("ida:RedirectUri")
                    Dim authority = String.Format("https://login.microsoftonline.com/{0}/v2.0", tenantId)

                    _app = ConfidentialClientApplicationBuilder.Create(clientId).
                        WithClientSecret(clientSecret).
                        WithRedirectUri(redirectUri).
                        WithAuthority(authority).
                        Build()
                End If
            End SyncLock
        End If
        Return _app
    End Function
End Module
