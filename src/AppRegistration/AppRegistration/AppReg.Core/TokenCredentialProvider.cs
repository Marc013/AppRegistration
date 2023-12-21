using AppRegistration.AppReg.Contracts;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AppRegistration.AppReg.Core
{
    internal class TokenCredentialProvider: ITokenCredentialProvider
    {
        private readonly IConfiguration _configuration;


        public TokenCredentialProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ClientSecretCredential? GetTokenForEnvironment(string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }
 
            var servicePrincipalDev = JsonSerializer.Deserialize<ServicePrincipalData>(_configuration[$"ServicePrincipal{environment}"]); // TODO: validate not null
            var servicePrincipalDevSecret = _configuration[$"ServicePrincipalSecret{environment}"]; // TODO: validate not null

            if (servicePrincipalDev is not null)
            {
                return new ClientSecretCredential(
                      servicePrincipalDev.AppId.ToString(),
                      servicePrincipalDev.TenantId.ToString(),
                      servicePrincipalDevSecret,
                            new ClientSecretCredentialOptions
                            {
                                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                            }
                );
            }
            else
            {
                return null;
            }
        }
    }
}
