using Azure.Identity;

namespace AppRegistration.AppReg.Contracts
{
    public interface ITokenCredentialProvider
    {
        public ClientSecretCredential? GetTokenForEnvironment(string environment);
    }
}
