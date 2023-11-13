﻿using AppRegistration.AppReg.Contracts;
using System.Text.RegularExpressions;
using static AppRegistration.AppReg.Core.AppRegistrationExceptions;

namespace AppRegistration.AppReg.Core
{
    internal partial class UniqueAppRegistrationName : IUniqueAppRegistrationName
    {
        private readonly IMsGraphServices _msGraphServices;

        public UniqueAppRegistrationName(IMsGraphServices msGrahpService)
        {
            _msGraphServices = msGrahpService;
        }

        [GeneratedRegex("(^|\\s)(.)")]
        private static partial Regex MyRegex();

        public async Task<string> GetUniqueAppRegistrationNameAsync(string prefix,
            string servicePrincipalApplicationId,
            string servicePrincipalTenantId,
            string servicePrincipalSecureSecret)
        {
            // VALIDATE PREFIX CONTAINS AT LEAST 2 HYPENS ('-')!
            var hypens = prefix.Count(x => x == '-');

            if (prefix.Count(x => x == '-') < 2)
            {
                throw new ArgumentException($"Provided prefix is incorrectly formatted as it does not contain 3 sections devided by a hypen.");
            }

            var prefixSections = prefix.Contains('-') ? prefix.Split('-') : prefix.Split(' ');

            var countryCode = prefixSections[0].Trim().ToUpper();

            var environmentCode = prefixSections[1].Trim().ToUpper();

            var name = MyRegex().Replace(string.Join(' ', prefixSections.Skip(2)).ToLower(), m => m.Groups[2].Value.ToUpper()).TrimStart();

            var namePrefix = $"{countryCode}-{environmentCode}-{name}";

            var allowedName = namePrefix.Length > 104 ? namePrefix[..104] : namePrefix;

            var count = 0;
            var uniqueName = "";
            bool retry = true;

            do
            {
                var uniqueString = Guid.NewGuid().ToString("N")[..15];
                uniqueName = $"{allowedName}-{uniqueString}";
                
                var msGraphClient = _msGraphServices.GetGraphClientWithServicePrincipalCredential(servicePrincipalApplicationId, servicePrincipalTenantId, servicePrincipalSecureSecret);

                var queryResult = await msGraphClient.Applications.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"startsWith(displayName, '{uniqueName}')";
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });

                if (queryResult!.OdataCount == 0)
                {
                    retry = false;
                }

                if (count >= 60)
                {
                    retry = false;
                }

                count++;
            }
            while (retry);

            if (count >= 60)
            {
                throw new UniqueAppRegistrationNameNotFoundException($"Unable to define a unique app registration name after {count} attempts.");
            }

            return uniqueName;
        }
    }
}
