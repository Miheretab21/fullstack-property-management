using System.Text.Json.Serialization;

namespace PropertyManagement.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionStatus
{
    Paid,
    Pending,
    Overdue
}
