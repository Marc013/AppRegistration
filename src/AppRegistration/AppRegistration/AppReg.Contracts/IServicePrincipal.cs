using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IServicePrincipal
    {
        Task<Microsoft.Graph.Models.ServicePrincipal> Create(GraphServiceClient msGraphClient, string name, string description,
            string appId, string notes);

        Task<AppRoleAssignment> AddRole(GraphServiceClient msGraphClient, string servicePrincipalId, string requesterId);
    }
}
