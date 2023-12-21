using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Azure.Core;
using Azure.Identity;
using Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IAppRegistrationNew, AppRegistrationNew>();
        services.AddSingleton<IKeyVault, KeyVault>();
        services.AddSingleton<IMsGraphServices, MsGraphServices>();
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IServiceBusCreateMessage, ServiceBusCreateMessage>();
        services.AddSingleton<IServicePrincipal, ServicePrincipal>();
        services.AddSingleton<IUniqueAppRegistrationName, UniqueAppRegistrationName>();
        services.AddSingleton<ITokenCredentialProvider, TokenCredentialProvider>();
    })
    .Build();

await host.WithIdentityLogging().RunAsync();
