using AppRegistration.AppReg.Contracts;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Azure.Security.KeyVault.Administration;
using System.Net;
using Azure.Core;
using System;

// Docs: https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net

namespace AppRegistration.AppReg.Core
{
    public class KeyVault : IKeyVault
    {
        private readonly ILogger<KeyVault> _logger;
        private readonly ITokenCredentialProvider _TokenCredentialProvider;

        public KeyVault(ILogger<KeyVault> logger, ITokenCredentialProvider tokenCredentialProvider)
        {
            _logger = logger;
            _TokenCredentialProvider = tokenCredentialProvider;
        }

        public async Task<KeyVaultSecret?> GetSecret(string environment, string keyVaultName, string secretName)
        {
            string kvUri = $"https://{keyVaultName}.vault.azure.net";

            var client = new SecretClient(new Uri(kvUri), _TokenCredentialProvider.GetTokenForEnvironment(environment));

            try
            {
                var secret = await client.GetSecretAsync(secretName);

                return secret.Value;
                //return secret.Value.Value;  /? TODO: Remove once this method is used.
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error retrieving secret from key vault");
                return null;
            }
        }

        public async Task<KeyVaultSecret?> AddSecret(string environment, string keyVaultName, string secretName, string secretValue)
        {
            string kvUri = $"https://{keyVaultName}.vault.azure.net";

            var client = new SecretClient(new Uri(kvUri), _TokenCredentialProvider.GetTokenForEnvironment(environment));

            try
            {
                var addSecret = await client.SetSecretAsync(secretName, secretValue);

                return addSecret.Value;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error adding secret to key vault");
                return null;
            }
        }

        public async Task<KeyVaultRoleAssignment?> AddRole(string environment, string roleId, string keyVaultName, string secretName,
            string principalObjectId)
        {
            // Docs:
            // https://github.com/Azure/azure-sdk-for-net/blob/Azure.Security.KeyVault.Administration_4.3.0/sdk/keyvault/Azure.Security.KeyVault.Administration/samples/Sample1_RbacHelloWorldAsync.md
            // https://github.com/Azure/azure-sdk-for-net/blob/Azure.Security.KeyVault.Administration_4.3.0/sdk/keyvault/Azure.Security.KeyVault.Administration/samples/Sample2_RbacScopeAssignment.md

            string kvUri = $"https://{keyVaultName}.vault.azure.net";

            var client = new KeyVaultAccessControlClient(new Uri(kvUri), _TokenCredentialProvider.GetTokenForEnvironment(environment));

            try
            {
                var secret = await GetSecret(keyVaultName, secretName);

                var createdAssignment = await client.CreateRoleAssignmentAsync(new KeyVaultRoleScope(secret.Id), roleId, principalObjectId);

                return createdAssignment.Value;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error setting role on key vault secret");
                return null;

            }
        }
    }
}
