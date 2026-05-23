using OpsLedger.Core.ServiceRequests.Constants;

namespace OpsLedger.Core.ServiceRequests.Entities;

public sealed class ServiceRequest
{
    public ServiceRequest(
        string title,
        RequestCategory category,
        RequestPriority priority,
        string description,
        string requesterName,
        string requesterEmail,
        string? impactDetails,
        RequestStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset slaDueAt,
        IReadOnlyList<RequestActivity> activity,
        IReadOnlyList<RequestComment>? comments = null,
        string? assigneeName = null,
        string? resolutionNotes = null)
    {
        Title = title;
        Category = category;
        Priority = priority;
        Description = description;
        RequesterName = requesterName;
        RequesterEmail = requesterEmail;
        ImpactDetails = impactDetails;
        Status = status;
        CreatedAt = createdAt;
        SlaDueAt = slaDueAt;
        Activity = activity;
        Comments = comments ?? [];
        AssigneeName = assigneeName;
        ResolutionNotes = resolutionNotes;
    }

    public string Title { get; }

    public RequestCategory Category { get; }

    public RequestPriority Priority { get; }

    public string Description { get; }

    public string RequesterName { get; }

    public string RequesterEmail { get; }

    public string? ImpactDetails { get; }

    public RequestStatus Status { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SlaDueAt { get; }

    public string? AssigneeName { get; }

    public string? ResolutionNotes { get; }

    public IReadOnlyList<RequestActivity> Activity { get; }

    public IReadOnlyList<RequestComment> Comments { get; }
}
