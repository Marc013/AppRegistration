using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IAppRegistrationNew
    {
        Task<Application> CreateAppRegistration(GraphServiceClient msGraphClient, string name, string description, string notes);

        Task<PasswordCredential> addPassword(GraphServiceClient msGraphClient, string applicationId, int validity = 1);
    }
}
