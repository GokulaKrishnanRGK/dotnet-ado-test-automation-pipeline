namespace OpsLedger.Api.Modules.ServiceRequests.Dto;

public sealed record CreateServiceRequestApiRequest(
    string Title,
    string Category,
    string Priority,
    string Description,
    string RequesterName,
    string RequesterEmail,
    string? ImpactDetails);
