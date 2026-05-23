namespace OpsLedger.Presentation.ServiceRequests.Dto;

public sealed record ServiceRequestSummary(
    string Id,
    string Title,
    string Category,
    string Priority,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    string? AssigneeName = null,
    string? ResolutionNotes = null,
    IReadOnlyList<ServiceRequestComment>? Comments = null,
    IReadOnlyList<string>? Activity = null);
