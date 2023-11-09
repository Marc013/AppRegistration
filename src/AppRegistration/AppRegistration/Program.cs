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
    })
    .Build();

await host.WithIdentityLogging().RunAsync();
