using OpsLedger.Core.ServiceRequests.Constants;

namespace OpsLedger.Core.ServiceRequests.Dto;

public sealed record CreateServiceRequestCommand(
    string Title,
    RequestCategory Category,
    RequestPriority Priority,
    string Description,
    string RequesterName,
    string RequesterEmail,
    string? ImpactDetails);
