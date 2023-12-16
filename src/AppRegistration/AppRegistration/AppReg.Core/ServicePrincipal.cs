using Microsoft.Graph;
using Microsoft.Graph.Models;
using AppRegistration.AppReg.Contracts;

namespace AppRegistration.AppReg.Core
{
    internal class ServicePrincipal : IServicePrincipal
    {
        public async Task<Microsoft.Graph.Models.ServicePrincipal> Create(GraphServiceClient msGraphClient, string name, string description,
            string appId, string notes)
        {

            var requestBody = new Microsoft.Graph.Models.ServicePrincipal
            {
                AccountEnabled = true,
                AppId = appId,
                DisplayName = name,
                Description = description,
                Notes = notes,
                AppRoleAssignmentRequired = true,               
            };

            var result = await msGraphClient.ServicePrincipals.PostAsync(requestBody);

            return result!;
        }

        public async Task<AppRoleAssignment> AddRole(GraphServiceClient msGraphClient, string servicePrincipalId, string requesterId)
        {
            var requestBody = new AppRoleAssignment
            {
                // The principal is assigned to the resource app without any specific app roles. https://learn.microsoft.com/en-us/graph/api/resources/approleassignment?view=graph-rest-1.0#properties
                AppRoleId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                PrincipalId = Guid.Parse(requesterId),
                ResourceId = Guid.Parse(servicePrincipalId),
            };

            var result = await msGraphClient.ServicePrincipals[servicePrincipalId].AppRoleAssignments.PostAsync(requestBody);

            return result!;
        }
    }
}
