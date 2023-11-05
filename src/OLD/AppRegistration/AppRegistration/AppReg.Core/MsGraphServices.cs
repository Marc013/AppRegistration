using AppRegistration.AppReg.Contracts;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

/* Docs:
    https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=csharp#using-a-client-secret
    https://github.com/damienbod/MicrosoftGraphAppToAppSecurity#using-graph-sdk-with-certificates-or-secrets
*/

namespace AppRegistration.AppReg.Core
{
    public class MsGraphServices : IMsGraphServices
    {
        private readonly ILogger<MsGraphServices> _logger;

        public MsGraphServices(ILogger<MsGraphServices> logger)
        {
            _logger = logger;
        }

        public GraphServiceClient GetGraphClientWithServicePrincipalCredential(string applicationId, string directoryId, string applicationSecret)
        {
            // The client credentials flow requires that you request the
            // /.default scope, and pre-configure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // Values from app registration
            var clientId = applicationId;
            var tenantId = directoryId;
            var clientSecret = applicationSecret;

            // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);

            return _graphServiceClient;
        }
    }
}
