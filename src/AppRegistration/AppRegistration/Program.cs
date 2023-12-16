using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => {
        services.AddSingleton<IMsGraphServices, MsGraphServices>();
        services.AddSingleton<IUniqueAppRegistrationName, UniqueAppRegistrationName>();
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IServiceBusCreateMessage, ServiceBusCreateMessage>();
        services.AddSingleton<IAppRegistrationNew, AppRegistrationNew> ();
        services.AddSingleton<IServicePrincipal, ServicePrincipal> ();
    })
    .Build();

await host.WithIdentityLogging().RunAsync();
