using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using AppRegistration.AppReg.Contracts;

// docs: https://learn.microsoft.com/en-us/entra/identity-platform/reference-app-manifest

namespace AppRegistration.AppReg.Core
{
    public class AppRegistrationNew : IAppRegistrationNew
    {
        public async Task<Application> CreateAppRegistration(GraphServiceClient msGraphClient, string name, string description)
        {
            var app = new Application
            {
                DisplayName = name,
                Description = description,
                SignInAudience = "AzureADMyOrg",
                RequiredResourceAccess = new List<RequiredResourceAccess>
                {
                    new RequiredResourceAccess
                    {
                        // Microsoft Graph https://learn.microsoft.com/en-us/troubleshoot/azure/active-directory/verify-first-party-apps-sign-in#application-ids-of-commonly-used-microsoft-applications
                        ResourceAppId = "00000003-0000-0000-c000-000000000000",
                        ResourceAccess = new List<ResourceAccess>
                        {
                            new ResourceAccess
                            {
                                // User.Read https://learn.microsoft.com/en-us/graph/migrate-azure-ad-graph-permissions-differences#userread
                                Id = new Guid("e1fe6dd8-ba31-4d61-89e7-88639da4683d"),
                                Type = "Scope"
                            }
                        }
                    }
                },
                Notes = "This is my new app registration"
            };

            var createdApp = await msGraphClient.Applications.PostAsync(app);

            return createdApp!;
        }

        public async Task<PasswordCredential> addPassword(GraphServiceClient msGraphClient, string applicationId, int validity = 1)
        {
            var requestBody = new AddPasswordPostRequestBody
            {
                PasswordCredential = new PasswordCredential
                {
                    DisplayName = "Secret",
                    EndDateTime = DateTimeOffset.UtcNow.AddYears(validity),
                    StartDateTime = DateTimeOffset.Now,
                },
            };

            var result = await msGraphClient.Applications[applicationId].AddPassword.PostAsync(requestBody);

            return result!;
        }
    }
}
