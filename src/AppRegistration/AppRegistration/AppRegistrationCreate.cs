using System;
using System.Text.Json;
using AppRegistration.AppReg.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AppRegistration
{
    public class AppRegistrationCreate
    {
        private readonly ILogger<AppRegistrationCreate> _logger;

        public AppRegistrationCreate(ILogger<AppRegistrationCreate> logger)
        {
            _logger = logger;
        }

        [Function(nameof(AppRegistrationCreate))]
        public void Run([ServiceBusTrigger("AppRegistrationCreate", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            try
            {
                var instrumentationMethodKey = "applicationregistrationcreation";
                var serviceBusMessageId = message.MessageId;
                var keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");

                AppRegistrationCreatePayload ? appRegistrationCreatePayload = JsonSerializer.Deserialize<AppRegistrationCreatePayload>(message.Body);

                var appRegistrationName = appRegistrationCreatePayload?.Workload.AppRegName;
                var appRegistrationdDescription = appRegistrationCreatePayload?.Workload.AppRegDescription;
                var environment = appRegistrationCreatePayload?.Workload.Environment.ToUpper().Replace("MANAGEMENT", "");
                var permissionType = appRegistrationCreatePayload?.Workload.Permission.GetType().GetProperties()[0].Name; // This should be "delegated"
                var requester = appRegistrationCreatePayload?.Workload.Requester;
                var callbackEndpoint = appRegistrationCreatePayload?.Workload.CallbackEndpoint;
                var ticketNumber = appRegistrationCreatePayload?.Workload.TicketNumber;

                _logger.LogInformation("Message ID: {id}", serviceBusMessageId);
                _logger.LogInformation("appRegistrationName: {appRegName}", appRegistrationName);
                _logger.LogInformation("appRegistrationNameDescription: {appRegDescription}", appRegistrationdDescription);
                _logger.LogInformation("environment: {environment}", environment);
                _logger.LogInformation("keyVaultName: {keyVaultName}", keyVaultName);
                _logger.LogInformation("permissionType: {permissionType}", permissionType);

                if (permissionType?.ToString() == "Delegated") // This should be "delegated"
                {
                    _logger.LogInformation("permission: {delegated}", appRegistrationCreatePayload?.Workload.Permission.Delegated); // dynamic with var?
                }
                else
                {
                    _logger.LogError("Unexpected permission type. Expected 'delegated' but received '{permissionType}'", permissionType);
                }
                _logger.LogInformation("requester: {requester}", requester);
                _logger.LogInformation("callbackEndpoint: {callbackEndpoint}", callbackEndpoint);
                _logger.LogInformation("ticketNumber: {ticketNumber}", ticketNumber);

                var environmentServicePrinicpal = Environment.GetEnvironmentVariable($"ServicePrincipal{environment}");

                if (string.IsNullOrWhiteSpace(environmentServicePrinicpal))
                {
                    var environments = new string[] { "Marc013", "Test" };

                    var allowedEnvironments = AllowedEnvironments.GetAllowedEnvironments(environments);
                    var executionMessage = $"FORBIDDEN - Request for creating an app registration in an above level tenant is not allowed. Allowed tenant(s) are '{allowedEnvironments}'";
                    var executionStatus = "Failed";

                    _logger.LogInformation(executionMessage);

                    return;
                }

                var servicePrincipal = JsonSerializer.Deserialize<ServicePrincipalData>(environmentServicePrinicpal);

                var keyVaultNameTargetTenant = servicePrincipal?.KeyVaultName;
                var servicePrincipalApplicationId = servicePrincipal?.AppId;
                var servicePrincipalName = servicePrincipal?.Name;
                var servicePrincipalTenantId = servicePrincipal?.TenantId;
                var servicePrincipalSecureSecret = Environment.GetEnvironmentVariable("ServicePrincipalSecretMarc013"); // DEOS NOT RETRIEVE SECRET FROM KEYVAULT :-(

                _logger.LogInformation("keyVaultNameTargetTenant: {keyVaultNameTargetTenant}", keyVaultNameTargetTenant); // FOR TESTING
            }
            finally
            {
                // If config did not complete successfully:
                // - remove created app registration 
                // - remove created app registration and key vault secret 

                // Send message to next function ;-)
            }
        }
    }
}
