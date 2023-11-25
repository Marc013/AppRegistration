using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRegistration.AppReg.Contracts
{
    internal interface IServiceBusCreateMessage
    {
        string ServiceBusCreateQueueMessage(string integrationMethodKey, string status, string message, string ticketNumber, string callbackEndpoint);
    }
}
