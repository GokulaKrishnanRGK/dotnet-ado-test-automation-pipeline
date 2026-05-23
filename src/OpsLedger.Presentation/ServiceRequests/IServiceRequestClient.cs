using OpsLedger.Presentation.ServiceRequests.Dto;

namespace OpsLedger.Presentation.ServiceRequests;

public interface IServiceRequestClient
{
    Task<ServiceRequestClientResult> CreateAsync(
        CreateServiceRequestInput request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestSummary>> ListAsync(
        ServiceRequestFilter filter,
        CancellationToken cancellationToken = default);
}
