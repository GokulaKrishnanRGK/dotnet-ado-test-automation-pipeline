namespace OpsLedger.Presentation.ServiceRequests.Dto;

public sealed record ServiceRequestSummary(
    string Id,
    string Title,
    string Category,
    string Priority,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt);
