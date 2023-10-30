using System.Text.Json;
using AppRegistration.AppReg.Contracts;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AppRegistration
{
    internal class AppRegistrationCreate
    {
        private readonly ILogger<AppRegistrationCreate> _logger;
        private readonly IKeyVaultService _keyVaultService;

        public AppRegistrationCreate(ILogger<AppRegistrationCreate> logger,
            IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _keyVaultService = keyVaultService;
        }

        [Function(nameof(AppRegistrationCreate))]
        public async Task Run([ServiceBusTrigger("AppRegistrationCreate", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            /*
            1. verify message JSON data against schema
                -- No clue. Perhaps using https://codepal.ai/code-generator/query/dl6piAuh/validate-json-schema
            2. create unique name
                2.1 create name ending with random alohanumeric string (15 char)
                2.2 validate if name exists in Entra ID, when yes --> repeast above
                2.3 create app registration (and enterprise application - or are these separate actions?)
            3. register app registration secret in key vault
                3.1 connect to key vault
                3.2 set key vault secret with expiration datetime
                3.3 assign requester role 'key vault secret user'
            4. That will follow later, first focus on the above
            */

            AppRegistrationPayload? appRegistrationPayload = JsonSerializer.Deserialize<AppRegistrationPayload>(message.Body);

            var KeyVaultName = Environment.GetEnvironmentVariable("KEY_VAULT_NAME");
            var ServicePrinicpalName = Environment.GetEnvironmentVariable("ServicePrinicpalName");

            if (string.IsNullOrWhiteSpace(KeyVaultName))
            {
                _logger.LogError("Missing value environment variable 'KEY_VAULT_NAME'");
                return;
            }

            if (string.IsNullOrWhiteSpace(ServicePrinicpalName))
            {
                _logger.LogError("Missing value environment variable 'ServicePrinicpalName'");
                return;
            }

            var secret = await _keyVaultService.GetKeyVaultSecretAsync(KeyVaultName, ServicePrinicpalName);

            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError("Error retrieving secret from key vault {KeyVaultName}", KeyVaultName);
                return;
            }

            var guid = Guid.NewGuid().ToString();
            var nameSuffix = guid.Replace("-", "").Substring(0, 15);
            _logger.LogInformation("nameSuffix is: {suffix}", nameSuffix);

            var appRegistrationName = $"{appRegistrationPayload?.Name}{nameSuffix}";

            _logger.LogInformation("appRegistrationName is: {appRegName}", appRegistrationName);

            // try to get the app registration using the new name. If it doesn't exist it can be created.
            // https://learn.microsoft.com/en-us/graph/api/application-get?view=graph-rest-1.0&tabs=csharp
        }
    }

    public class AppRegistrationPayload
    {
        public required string Name { get; set; }
        public required string Message { get; set; }
    }
}
