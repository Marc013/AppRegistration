using Microsoft.Graph;
using System.Threading.Tasks;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IMsGraphServices
    {
        GraphServiceClient GetGraphClientWithServicePrincipalCredential(string applicationId, string directoryId, string applicationSecret);
    }
}
