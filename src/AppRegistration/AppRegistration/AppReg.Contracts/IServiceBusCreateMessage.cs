namespace AppRegistration.AppReg.Contracts
{
    internal interface IServiceBusCreateMessage
    {
        string ServiceBusCreateQueueMessage(string integrationMethodKey, string status, string message, string ticketNumber, string callbackEndpoint);
    }
}
