namespace OpsLedger.Infrastructure.Persistence.Entities;

public sealed class ServiceRequestRecord
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string? ImpactDetails { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset SlaDueAt { get; set; }
    public string? AssigneeName { get; set; }
    public string? ResolutionNotes { get; set; }

    public List<RequestActivityRecord> Activity { get; set; } = [];
    public List<RequestCommentRecord> Comments { get; set; } = [];
}
