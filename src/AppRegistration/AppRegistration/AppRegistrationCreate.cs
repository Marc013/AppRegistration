using System.Text.Json;
using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using static AppRegistration.AppReg.Core.AppRegistrationExceptions;

namespace AppRegistration
{
    internal class AppRegistrationCreate
    {
        private readonly ILogger<AppRegistrationCreate> _logger;

        private readonly IAppRegistrationNew _appRegistrationNew;
        private readonly IKeyVault _keyVault;
        private readonly IMsGraphServices _msGraphServices;
        private readonly IServiceBusService _serviceBusService;
        private readonly IServiceBusCreateMessage _serviceBusCreateMessage;
        private readonly IServicePrincipal _servicePrincipal;
        private readonly IUniqueAppRegistrationName _uniqueAppRegistrationName;

        public AppRegistrationCreate(ILogger<AppRegistrationCreate> logger,
            IAppRegistrationNew appRegistrationNew,
            IKeyVault keyVault,
            IMsGraphServices msGraphService,
            IServiceBusService serviceBusService,
            IServiceBusCreateMessage serviceBusCreateMessage,
            IServicePrincipal servicePrincipal,
            IUniqueAppRegistrationName uniqueAppRegistrationName)
        {
            _appRegistrationNew = appRegistrationNew;
            _keyVault = keyVault;
            _logger = logger;
            _msGraphServices = msGraphService;
            _serviceBusService = serviceBusService;
            _serviceBusCreateMessage = serviceBusCreateMessage;
            _servicePrincipal = servicePrincipal;
            _uniqueAppRegistrationName = uniqueAppRegistrationName;
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

                var environmentServicePrincipal = Environment.GetEnvironmentVariable($"ServicePrincipal{environment}");

                if (environmentServicePrincipal is null)
                {
                    var environments = new string[] { "Marc013", "Test" }; // SHOULD BE PART OF THE CONFIGURATION

                    var allowedEnvironments = AllowedEnvironments.GetAllowedEnvironments(environments);
                    
                    executionMessage = $"FORBIDDEN - Request for creating an app registration in an above level tenant is not allowed. Allowed tenant(s) are '{allowedEnvironments}'";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                }

                var servicePrincipal = JsonSerializer.Deserialize<ServicePrincipalData>(environmentServicePrincipal!);

                var keyVaultNameTargetTenant = servicePrincipal?.KeyVaultName;
                var servicePrincipalApplicationId = servicePrincipal?.AppId.ToString();
                var servicePrincipalName = servicePrincipal?.Name;
                var servicePrincipalTenantId = servicePrincipal?.TenantId.ToString();
                var servicePrincipalSecureSecret = Environment.GetEnvironmentVariable($"ServicePrincipalSecret{environment}"); // "ServicePrincipalSecretMarc013"

                if (keyVaultNameTargetTenant is null)
                {
                    executionMessage = "Environment service principal key vault name not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if(servicePrincipalApplicationId is null)
                {
                    executionMessage = "Environment service principal application ID not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if(servicePrincipalName is null)
                {
                    executionMessage = "Environment service principal name not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if(servicePrincipalTenantId is null)
                {
                    executionMessage = "Environment service principal tenant ID not found in app configuration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);
                    // Inform the function developer about this technical issue
                }

                if (servicePrincipalSecureSecret is null)
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

                if (entraIdUser is null || entraIdUser.UserPrincipalName != requester) // CHECK IF THIS CHECK ALSO WORKS WHEN THE REQUESTER IS NOT FOUND
                {
                    executionMessage = $"Requester {requester} not found in tenant {environment}";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                // Get unique App registration name using 'appRegistrationNamePrefix' GetUniqueApplicationRegistrationName
                _logger.LogInformation("Generating unique app registration name");
                var uniqueAppRegistrationName = await _uniqueAppRegistrationName.GetUniqueAppRegistrationNameAsync(appRegistrationNamePrefix!, servicePrincipalApplicationId!, servicePrincipalTenantId!, servicePrincipalSecureSecret!);

                _logger.LogInformation("uniqueAppRegistrationName: {uniqueAppRegistrationName}", uniqueAppRegistrationName);

                _logger.LogInformation("Creating app registration");
                var notes = @"{""requester"": """ + requester + @""", ""ticketNumber"": """ + ticketNumber + @"""}";

                var newAppRegistration = await _appRegistrationNew.CreateAppRegistration(msGraphClient, uniqueAppRegistrationName,
                    appRegistrationDescription!, notes);

                if (newAppRegistration.DisplayName != uniqueAppRegistrationName)
                {
                    executionMessage = $"Failed to create app registration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                _logger.LogInformation("Adding password to app registration");
                var appRegistrationPassword = await _appRegistrationNew.addPassword(msGraphClient, newAppRegistration.Id!);

                if (appRegistrationPassword.SecretText is null)
                {
                    executionMessage = $"Failed to add password to app registration";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                _logger.LogInformation("Creating service principal");

                var newServicePrincipal = await _servicePrincipal.Create(msGraphClient, uniqueAppRegistrationName, appRegistrationDescription!, newAppRegistration.AppId!, notes);

                if (newServicePrincipal.DisplayName != uniqueAppRegistrationName)
                {
                    executionMessage = $"Failed to create service principal";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                _logger.LogInformation("Assigning requester to service principal");
                var addRole = await _servicePrincipal.AddRole(msGraphClient, newServicePrincipal.Id!, entraIdUser!.Id!);

                if (addRole.ResourceDisplayName != uniqueAppRegistrationName)
                {
                    executionMessage = $"Failed to add the requester as user to the service principal";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop
                }

                _logger.LogInformation("Adding app registration secret to key vault {keyVaultName}", keyVaultName);
                var setSecretInKeyVault = await _keyVault.AddSecret(environment!, keyVaultName!, uniqueAppRegistrationName, appRegistrationPassword.SecretText!, 7);

                if (setSecretInKeyVault is null)
                {
                    executionMessage = $"Failed to add app registration secret to key vault";
                    executionStatus = "Failed";
                    _logger.LogError("{executionMessage}", executionMessage);

                    // throw exception to stop AND clean up
                }

                // Docs: https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide?tabs=azure-cli#azure-built-in-roles-for-key-vault-data-plane-operations
                _logger.LogInformation("Setting role 'Key Vault Secrets User' on app registration secret");
                var setKeyVaultSecretRoll = await _keyVault.AddRole(environment!, "4633458b-17de-408a-b874-0445c86b69e6", keyVaultName!, setSecretInKeyVault!.Name, entraIdUser.Id!);
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
                    else
                    {
                        executionMessage = ex.Message;
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
                var hypenErrorMessage = "Provided prefix is incorrectly formatted as it does not contain 3 sections divided by a hyphen.";
                if (ex.Message == hypenErrorMessage)
                {
                    // HANDLE THIS!
                }
                else
                {
                    // WHAT NOW??
                }
            }
            catch (UniqueAppRegistrationNameNotFoundException ex)
            {
                executionStatus = "Failed";

                executionMessage = ex.Message;
                _logger.LogError("{type}: {message}", ex.GetType().Name, executionMessage);
                _logger.LogError("{executionMessage}", executionMessage);
            }
            catch (Exception ex) // FOR TESTING
            {
                executionStatus = "Failed";

                executionMessage = ex.Message;
                _logger.LogError("{type}: {message}", ex.GetType().Name, executionMessage);
                _logger.LogError("{executionMessage}", executionMessage);
            }
            finally
            {
                // If config did not complete successfully:
                // - remove created app registration 
                // - remove created app registration and key vault secret 
                if (executionStatus == "Failed")
                {
                    // Check executionMessage to determine what to clean up.
                    _logger.LogInformation("FUNCTION APP EXECUTION FAILED!!"); // TODO: REMOVE THIS LINE

                }

                var callbackMessage = _serviceBusCreateMessage.ServiceBusCreateQueueMessage(instrumentationMethodKey!, executionStatus,
                    executionMessage!, ticketNumber!, callbackEndpoint!);

                await _serviceBusService.SendQueueMessage(callbackMessage);
            }
        }
    }
}
