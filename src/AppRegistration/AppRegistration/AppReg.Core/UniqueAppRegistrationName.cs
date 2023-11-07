using AppRegistration.AppReg.Contracts;
using System.Text.RegularExpressions;

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
            var prefixSections = prefix.Contains('-') ? prefix.Split('-') : prefix.Split(' ');

            var countryCode = prefixSections[0].Trim().ToUpper();

            var environmentCode = prefixSections[1].Trim().ToUpper();

            var name = MyRegex().Replace(string.Join(' ', prefixSections.Skip(2)).ToLower(), m => m.Groups[2].Value.ToUpper()).TrimStart();

            var namePrefix = $"{countryCode}-{environmentCode}-{name}";

            var allowedName = namePrefix.Length > 104 ? namePrefix[..104] : namePrefix;

            var count = 0;
            var uniqueName = "";
            string? existingName = null;

            do
            {
                var uniqueString = Guid.NewGuid().ToString().Remove('-')[..15];
                uniqueName = $"{allowedName}-{uniqueString}";

                var msGraphClient = _msGraphServices.GetGraphClientWithServicePrincipalCredential(servicePrincipalApplicationId, servicePrincipalTenantId, servicePrincipalSecureSecret);

                var queryResult = await msGraphClient.Applications.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"eq(displayName, {servicePrincipalTenantId})";
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });

                if (!string.IsNullOrEmpty(queryResult!.Value![0].AppId))
                {
                    existingName = queryResult!.Value![0].AppId!.ToString();
                }
            }
            while (string.IsNullOrWhiteSpace(existingName) | count <= 60);

            return uniqueName;
        }
    }
}
