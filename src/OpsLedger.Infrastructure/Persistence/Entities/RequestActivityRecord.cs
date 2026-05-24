namespace OpsLedger.Infrastructure.Persistence.Entities;

public sealed class RequestActivityRecord
{
    public long Id { get; set; }
    public string ServiceRequestId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
}
