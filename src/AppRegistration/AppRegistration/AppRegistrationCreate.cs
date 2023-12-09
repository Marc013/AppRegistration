using System.Text.Json;
using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using static AppRegistration.AppReg.Core.AppRegistrationExceptions;

namespace AppRegistration
{
    internal class AppRegistrationCreate
    {
        private readonly ILogger<AppRegistrationCreate> _logger;

        private readonly IMsGraphServices _msGraphServices;
        private readonly IUniqueAppRegistrationName _uniqueAppRegistrationName;
        private readonly IServiceBusService _serviceBusService;
        private readonly IServiceBusCreateMessage _serviceBusCreateMessage;
        private readonly IAppRegistrationNew _appRegistrationNew;

        public AppRegistrationCreate(ILogger<AppRegistrationCreate> logger,
            IMsGraphServices msGraphService,
            IUniqueAppRegistrationName uniqueAppRegistrationName,
            IServiceBusService serviceBusService,
            IServiceBusCreateMessage serviceBusCreateMessage,
            IAppRegistrationNew appRegistrationNew)
        {
            _logger = logger;
            _msGraphServices = msGraphService;
            _uniqueAppRegistrationName = uniqueAppRegistrationName;
            _serviceBusService = serviceBusService;
            _serviceBusCreateMessage = serviceBusCreateMessage;
            _appRegistrationNew = appRegistrationNew;
        }

        [Function(nameof(AppRegistrationCreate))]
        public async Task RunAsync([ServiceBusTrigger("AppRegistrationCreate", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            string? callbackEndpoint = null;
            string executionStatus = "Succeeded";
            string? executionMessage = "dummy message";
            string? instrumentationMethodKey = null;
            string? requester = null;
            string? ticketNumber = null;

            try
            {
                instrumentationMethodKey = "applicationregistrationcreation";
                var serviceBusMessageId = message.MessageId;
                var keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");

                AppRegistrationCreatePayload? appRegistrationCreatePayload = JsonSerializer.Deserialize<AppRegistrationCreatePayload>(message.Body);

                // ToDo: validate payload. None of the values should be missing!

                var appRegistrationNamePrefix = appRegistrationCreatePayload?.Workload.AppRegName;
                var appRegistrationDescription = appRegistrationCreatePayload?.Workload.AppRegDescription;
                var environment = appRegistrationCreatePayload?.Workload.Environment.ToUpper().Replace("MANAGEMENT", "");
                var permissionType = appRegistrationCreatePayload?.Workload.Permission.GetType().GetProperties()[0].Name; // This should be "delegated"
                requester = appRegistrationCreatePayload?.Workload.Requester;
                callbackEndpoint = appRegistrationCreatePayload?.Workload.CallbackEndpoint;
                ticketNumber = appRegistrationCreatePayload?.Workload.TicketNumber;

                _logger.LogInformation("instrumentationMethodKey: {instrumentationMethodKey}", instrumentationMethodKey);
                _logger.LogInformation("serviceBusMessageId: {serviceBusMessageId}", serviceBusMessageId);
                _logger.LogInformation("appRegistrationNamePrefix: {appRegName}", appRegistrationNamePrefix);
                _logger.LogInformation("appRegistrationNameDescription: {appRegDescription}", appRegistrationDescription);
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
                    var environments = new string[] { "Marc013", "Test" }; // SHOULD BE PART OF THE CONFIGURATION

                    var allowedEnvironments = AllowedEnvironments.GetAllowedEnvironments(environments);
                    executionMessage = $"FORBIDDEN - Request for creating an app registration in an above level tenant is not allowed. Allowed tenant(s) are '{allowedEnvironments}'";
                    executionStatus = "Failed";

                    _logger.LogError("{executionMessage}", executionMessage);

                    // Send message to queue
                }

                var servicePrincipal = JsonSerializer.Deserialize<ServicePrincipalData>(environmentServicePrinicpal!);

                var keyVaultNameTargetTenant = servicePrincipal?.KeyVaultName;
                var servicePrincipalApplicationId = servicePrincipal?.AppId.ToString();
                var servicePrincipalName = servicePrincipal?.Name;
                var servicePrincipalTenantId = servicePrincipal?.TenantId.ToString();
                var servicePrincipalSecureSecret = Environment.GetEnvironmentVariable("ServicePrincipalSecretMarc013");

                if (string.IsNullOrWhiteSpace(keyVaultNameTargetTenant))
                {
                    executionMessage = "Environment service principal key vault name not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalApplicationId))
                {
                    executionMessage = "Environment service principal application ID not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalName))
                {
                    executionMessage = "Environment service principal name not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if (string.IsNullOrWhiteSpace(servicePrincipalTenantId))
                {
                    executionMessage = "Environment service principal tenant ID not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if (string.IsNullOrEmpty(servicePrincipalSecureSecret))
                {
                    executionMessage = $"Environment service principal secret (via key vault link) not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                // Get requester from Microsoft Entra ID. This to validate the correct UPN is provided.
                // This needs to be in the correct tenant!
                // 1. Create graph client for correct tenant!
                var msGraphClient = _msGraphServices.GetGraphClientWithServicePrincipalCredential(servicePrincipalApplicationId!, servicePrincipalTenantId!, servicePrincipalSecureSecret!);
                // 2. Get user
                var entraIdUser = await msGraphClient.Users[requester].GetAsync();

                if (entraIdUser is null)
                {
                    executionMessage = $"Requester {requester} not found in tenant {environment}";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                // Get unique App registration name using 'appRegistrationNamePrefix' GetUniqueApplicationRegistrationName
                var uniqueAppRegistrationName = await _uniqueAppRegistrationName.GetUniqueAppRegistrationNameAsync(appRegistrationNamePrefix!, servicePrincipalApplicationId!, servicePrincipalTenantId!, servicePrincipalSecureSecret!);

                _logger.LogInformation("uniqueAppRegistrationName: {uniqueAppRegistrationName}", uniqueAppRegistrationName);

                // Create app registration (and enterprise application - service principal)
                _logger.LogInformation("Creating app registration");
                var newAppRegistration = await _appRegistrationNew.CreateAppRegistration(msGraphClient, uniqueAppRegistrationName, appRegistrationDescription!);

                if (newAppRegistration is null)
                {
                    executionMessage = $"Failed to create app registration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                _logger.LogInformation("Adding password to app registration");
                var appRegistrationPassword = await _appRegistrationNew.addPassword(msGraphClient, newAppRegistration!.Id!);

                if (appRegistrationPassword is null)
                {
                    executionMessage = $"Failed to add password to app registration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }
                else
                {
                    _logger.LogInformation("Password: {password}", appRegistrationPassword.SecretText); // ONLY FOR TESTING!!
                }

            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);
                // Inform the function developer about this technical issue
            }
            catch (ODataError ex)
            {
                _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);

                if (ex.Error is not null)
                {
                    executionStatus = "Failed";

                    if (ex.Error.Code == "Request_ResourceNotFound")
                    {
                        executionMessage = $"Unable to find provided App registration owner '{requester}' in Microsoft Entra ID.";
                        _logger.LogError("{executionMessage}", executionMessage);
                    }
                }

                // TO MY RECOLLECTION THERE IS A 2ND ODATAERROR POSSIBLE!?
            }
            catch (ArgumentException ex)
            {
                executionStatus = "Failed";

                _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);
                // send message to queue
                var hyperErrorMessage = "Provided prefix is incorrectly formatted as it does not contain 3 sections divided by a hyphen.";
                if (ex.Message == hyperErrorMessage)
                {

                }
                else
                {
                    // WHAT NOW??
                }
            }
            catch (UniqueAppRegistrationNameNotFoundException ex)
            {
                executionStatus = "Failed";

                _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);
            }
            catch (Exception ex) // FOR TESTING
            {
                executionStatus = "Failed";

                _logger.LogError("{type}: {message}", ex.GetType().Name, ex.Message);
            }
            finally
            {
                // If config did not complete successfully:
                // - remove created app registration 
                // - remove created app registration and key vault secret 
                if (executionStatus == "Failed")
                {
                    // Check executionMessage to determine what to clean up.

                }

                var callbackMessage = _serviceBusCreateMessage.ServiceBusCreateQueueMessage(instrumentationMethodKey!, executionStatus, executionMessage!, ticketNumber!, callbackEndpoint!);

                var queueMessage = JsonSerializer.Serialize(callbackMessage);

                await _serviceBusService.SendQueueMessage(queueMessage);
            }
        }
    }
}
