using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => {
        services.AddSingleton<IAppRegistrationNew, AppRegistrationNew>();
        services.AddSingleton<IKeyVault, KeyVault>();
        services.AddSingleton<IMsGraphServices, MsGraphServices>();
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IServiceBusCreateMessage, ServiceBusCreateMessage>();
        services.AddSingleton<IServicePrincipal, ServicePrincipal> ();
        services.AddSingleton<IUniqueAppRegistrationName, UniqueAppRegistrationName>();
    })
    .Build();

await host.WithIdentityLogging().RunAsync();
