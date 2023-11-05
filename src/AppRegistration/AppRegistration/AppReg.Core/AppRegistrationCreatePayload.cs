using System.Text.Json.Serialization;

// This class is created using https://app.quicktype.io/
// nullable enable
#pragma warning disable CS8618

namespace AppRegistration.AppReg.Core
{
    public partial class AppRegistrationCreatePayload
    {
        [JsonPropertyName("workload")]
        public Workload Workload { get; set; }
    }

    public partial class Workload
    {
        [JsonPropertyName("appRegDescription")]
        public string AppRegDescription { get; set; }

        [JsonPropertyName("appRegName")]
        public string AppRegName { get; set; }

        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("permission")]
        public Permission Permission { get; set; }

        [JsonPropertyName("requester")]
        public string Requester { get; set; }

        [JsonPropertyName("ticketNumber")]
        public string TicketNumber { get; set; }

        [JsonPropertyName("callbackEndpoint")]
        public string CallbackEndpoint { get; set; }
    }

    public partial class Permission
    {
        [JsonPropertyName("delegated")]
        public List<string> Delegated { get; set; }
    }
}
