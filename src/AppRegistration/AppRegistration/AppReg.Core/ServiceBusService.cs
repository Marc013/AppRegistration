using AppRegistration.AppReg.Contracts;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

// Docs: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues

namespace AppRegistration.AppReg.Core
{
    public class ServiceBusService: IServiceBusService
    {
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(ILogger<ServiceBusService> logger)
        {
            _logger = logger;
        }

        public async Task SendQueueMessage(string message)
        {
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");
            var nameSpace = Environment.GetEnvironmentVariable("ServiceBusNamespace");

            ServiceBusClient client;

            ServiceBusSender sender;

            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };

            //client = new ServiceBusClient(
            //    nameSpace,
            //    new DefaultAzureCredential(),
            //    clientOptions); // SHOULD BE USING SMI

            var serviceBusConnection = Environment.GetEnvironmentVariable("serviceBusConnection");
            client = new ServiceBusClient(serviceBusConnection, clientOptions); // ONLY FOR TESTING!
            sender = client.CreateSender(queueName);

            try
            {
                await sender.SendMessageAsync(new ServiceBusMessage(message));
                _logger.LogInformation("Sending service bus queue message: {message}", message);
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
