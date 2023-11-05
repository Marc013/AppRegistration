using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IMsGraphServices
    {
        GraphServiceClient GetGraphClientWithServicePrincipalCredential(string applicationId, string directoryId, string applicationSecret);
    }
}
