using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

// Docs: https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net

namespace AppRegistration.AppReg.Core
{
    public class KeyVaultService
    {
        public static async Task<string> GetKeyVaultSecretAsync(string? keyVaultName, string? secretName)
        {
            if ((keyVaultName is not null) && (secretName is not null))
            {
                string kvUri = $"https://{keyVaultName}.vault.azure.net";

                var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                try
                {
                    var secret = await client.GetSecretAsync(secretName);

                    return secret.Value.Value;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else
            {
                return $"Missing value: keyVaultName is '{keyVaultName}', secretName is '{secretName}'";
            }
        }
    }
}
