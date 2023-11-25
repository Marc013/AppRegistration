namespace AppRegistration.AppReg.Contracts
{
    internal interface IServiceBusService
    {
        Task SendQueueMessage(string message);
    }
}
