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

            // name of your Service Bus queue
            // the client that owns the connection and can be used to create senders and receivers
            ServiceBusClient client;

            // the sender used to publish messages to the queue
            ServiceBusSender sender;

            // number of messages to be sent to the queue
            const int numOfMessages = 3;

            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //
            // Set the transport type to AmqpWebSockets so that the ServiceBusClient uses the port 443. 
            // If you use the default AmqpTcp, ensure that ports 5671 and 5672 are open.
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
                // sent single message
                await sender.SendMessageAsync(new ServiceBusMessage(message));
                _logger.LogInformation("Sending message '{message}' to the service bus queue.", message);
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
