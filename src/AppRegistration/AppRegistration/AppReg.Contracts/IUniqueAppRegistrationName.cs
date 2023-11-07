namespace AppRegistration.AppReg.Contracts
{
    internal interface IUniqueAppRegistrationName
    {
        Task<string> GetUniqueAppRegistrationNameAsync(string prefix,
            string servicePrincipalApplicationId,
            string servicePrincipalTenantId,
            string servicePrincipalSecureSecret);
    }
}
