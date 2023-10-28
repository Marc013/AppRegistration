using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

// Docs: https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net

namespace AppRegistration.AppReg.Core
{
    internal class AzureKeyVault (string secretName)
    {
        readonly string keyVaultName = "%KEY_VAULT_NAME%"; //Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
        readonly string kvUri = "https://" + keyVaultName + ".vault.azure.net";

        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

        var secret = await client.GetSecretAsync(secretName); // to be used in the function app itself
    }
}
