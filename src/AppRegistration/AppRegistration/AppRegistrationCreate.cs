using System;
using System.Net.Sockets;
using System.Text.Json;
using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AppRegistration
{
    internal class AppRegistrationCreate
    {
        private readonly ILogger<AppRegistrationCreate> _logger;

        private readonly IMsGraphServices _msGraphServices;

        public AppRegistrationCreate(ILogger<AppRegistrationCreate> logger,
            IMsGraphServices msGrahpService)
        {
            _logger = logger;
            _msGraphServices = msGrahpService;
        }

        [Function(nameof(AppRegistrationCreate))]
        public async Task RunAsync([ServiceBusTrigger("AppRegistrationCreate", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            try
            {
                var instrumentationMethodKey = "applicationregistrationcreation";
                var serviceBusMessageId = message.MessageId;
                var keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");

                AppRegistrationCreatePayload? appRegistrationCreatePayload = JsonSerializer.Deserialize<AppRegistrationCreatePayload>(message.Body);

                var appRegistrationName = appRegistrationCreatePayload?.Workload.AppRegName;
                var appRegistrationdDescription = appRegistrationCreatePayload?.Workload.AppRegDescription;
                var environment = appRegistrationCreatePayload?.Workload.Environment.ToUpper().Replace("MANAGEMENT", "");
                var permissionType = appRegistrationCreatePayload?.Workload.Permission.GetType().GetProperties()[0].Name; // This should be "delegated"
                var requester = appRegistrationCreatePayload?.Workload.Requester;
                var callbackEndpoint = appRegistrationCreatePayload?.Workload.CallbackEndpoint;
                var ticketNumber = appRegistrationCreatePayload?.Workload.TicketNumber;

                _logger.LogInformation("instrumentationMethodKey: {instrumentationMethodKey}", instrumentationMethodKey);
                _logger.LogInformation("serviceBusMessageId: {serviceBusMessageId}", serviceBusMessageId);
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
                var servicePrincipalApplicationId = servicePrincipal?.AppId.ToString();
                var servicePrincipalName = servicePrincipal?.Name;
                var servicePrincipalTenantId = servicePrincipal?.TenantId.ToString();
                var servicePrincipalSecureSecret = Environment.GetEnvironmentVariable("ServicePrincipalSecretMarc013"); // DOES NOT RETRIEVE SECRET FROM KEYVAULT :-(

                if (string.IsNullOrWhiteSpace(keyVaultNameTargetTenant))
                {
                    _logger.LogError("Environment service principal key vault name not present");
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalApplicationId))
                {
                    _logger.LogError("Environment service principal application ID not present");
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalName))
                {
                    _logger.LogError("Environment service principal name not present");
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalTenantId))
                {
                    _logger.LogError("Environment service principal tenant ID not present");
                }

                if (string.IsNullOrEmpty(servicePrincipalSecureSecret))
                {
                    _logger.LogError("Unable to retrieve the secret of service principal '{servicePrincipalName}' from key vault '{keyVaultName}'", servicePrincipalName, keyVaultName);

                    return; // IS THIS REQUIRED? WHEN YES, THAN IT NEEDS TO BE ADDED IN THE ABOVE IF STATEMENTS AS WELL!
                }

                // Get requester from Microsoft Entra ID. This to validate the correct UPN is provided.
                // This needs to be in the correct tenant!
                // 1. Create graph client for correct tenant!
                var msGraphClient = _msGraphServices.GetGraphClientWithServicePrincipalCredential(servicePrincipalApplicationId!, servicePrincipalTenantId!, servicePrincipalSecureSecret);
                // 2. Get user
                var entraIdUser = await msGraphClient.Users[requester].GetAsync();

                _logger.LogInformation("entraIdUser: {entraIdUser}", entraIdUser!.DisplayName);

            }
            catch (AggregateException ae)  // THIS DOES NOT WORK :-(
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);
                }
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
