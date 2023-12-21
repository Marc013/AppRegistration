using AppRegistration.AppReg.Contracts;
using System.Text.Json;

namespace AppRegistration.AppReg.Core
{
    public class ServiceBusCreateMessage : IServiceBusCreateMessage
    {
        public string ServiceBusCreateQueueMessage(string integrationMethodKey, string status, string message, string ticketNumber, string callbackEndpoint)
        {
            if (status != "Succeeded" && status != "Failed")
            {
                throw new ArgumentException($"Parameter 'status' only allows 'Succeeded' or 'Failed'.");
            }

            var queueMessage = new ServiceBusQueueMessage
            {
                IntegrationMethodKey = integrationMethodKey,
                Status = status,
                Message = message,
                TicketNumber = ticketNumber,
                CallbackEndpoint = callbackEndpoint
            };

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(queueMessage, options);

            return jsonString;
        }
    }
    internal class ServiceBusQueueMessage
    {
        public required string IntegrationMethodKey { get; set; }
        public required string Status { get; set; }
        //[JsonConverter(typeof(JsonStringEnumConverter))]
        public required string Message { get; set; }
        public required string TicketNumber { get; set; }
        public required string CallbackEndpoint { get; set; }
    }

}
