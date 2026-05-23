namespace OpsLedger.Presentation.ServiceRequests.Dto;

public sealed record CreateServiceRequestInput(
    string Title,
    string Category,
    string Priority,
    string Description,
    string RequesterName,
    string RequesterEmail,
    string? ImpactDetails);
