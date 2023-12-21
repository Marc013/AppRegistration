using Azure.Security.KeyVault.Administration;
using Azure.Security.KeyVault.Secrets;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IKeyVault
    {
        Task<KeyVaultSecret?> GetSecret(string environment, string keyVaultName, string secretName);

        Task<KeyVaultSecret?> AddSecret(string environment, string keyVaultName, string secretName, string secretValue);

        Task<KeyVaultRoleAssignment?> AddRole(string environment, string roleId, string keyVaultName, string secretName,
            string principalObjectId);
    }
}
