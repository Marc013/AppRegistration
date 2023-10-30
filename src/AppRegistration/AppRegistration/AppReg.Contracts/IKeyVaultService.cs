namespace AppRegistration.AppReg.Contracts
{
    internal interface IKeyVaultService
    {
        Task<string?> GetKeyVaultSecretAsync(string keyVaultName, string secretName);
    }
}
