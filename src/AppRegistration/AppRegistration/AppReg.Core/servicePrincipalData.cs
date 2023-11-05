using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppRegistration.AppReg.Core
{
    public class ServicePrincipalData
    {
        [JsonPropertyName("AppId")]
        public required Guid AppId { get; set; }

        [JsonPropertyName("KeyVaultName")]
        public required string KeyVaultName { get; set; }

        [JsonPropertyName("Name")]
        public required string Name { get; set; }

        [JsonPropertyName("TenantId")]
        public required Guid TenantId { get; set; }
    }
}
