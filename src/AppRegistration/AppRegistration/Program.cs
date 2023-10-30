using AppRegistration.AppReg.Contracts;
using AppRegistration.AppReg.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => {
        services.AddSingleton<IKeyVaultService, KeyVaultService>();
    })
    .Build();

host.Run();
