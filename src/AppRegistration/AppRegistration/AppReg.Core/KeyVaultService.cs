using AppRegistration.AppReg.Contracts;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

// Docs: https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net

namespace AppRegistration.AppReg.Core
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(ILogger<KeyVaultService> logger)
        {
            _logger = logger;
        }


        public async Task<string?> GetKeyVaultSecretAsync(string keyVaultName, string secretName)
        {
            if (string.IsNullOrWhiteSpace(keyVaultName) && string.IsNullOrWhiteSpace(secretName))
            {
                string kvUri = $"https://{keyVaultName}.vault.azure.net";

                var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                try
                {
                    var secret = await client.GetSecretAsync(secretName);

                    return secret.Value.Value;
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Error retrieving secret from key vault");
                    return null;
                }
            }
            
            _logger.LogError("Parmeter value missing value");
            return null;
        }
    }
}
