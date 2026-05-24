namespace OpsLedger.Infrastructure.Persistence.Entities;

public sealed class RequestCommentRecord
{
    public long Id { get; set; }
    public string ServiceRequestId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
