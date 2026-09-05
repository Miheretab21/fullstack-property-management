using System.Text.Json.Serialization;

namespace PropertyManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaintenanceStatus
{
    Open,
    InProgress,
    Closed
}
