using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;

namespace Core;

public static class ExtenstionMethods
{
    public static IHost WithIdentityLogging(this IHost host)
    {
        host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(async () =>
        {
            // Below does not work as function app runtime does not output the IlLoggerFactory to the console
            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            //var logger = loggerFactory.CreateLogger("Function.Core.User");
            //logger.LogInformation("Application started"); 

            var azureCredential = new DefaultAzureCredential();

            var creds = await azureCredential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            var jwt = creds.Token;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            var payloadsToLog = new[] { "tid", "oid", "preferred_username", "name", "email" };

            foreach (var payloadToLog in payloadsToLog)
            {
                if (token.Payload.TryGetValue(payloadToLog, out var payload))
                {
                    Console.Write(payloadToLog);
                    Console.Write(": ");
                    Console.WriteLine(payload);
                }
            }
        });

        return host;
    }
}
