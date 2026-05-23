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

    Task<ServiceRequestSummary?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestClientResult> AssignAsync(
        string id,
        AssignServiceRequestInput request,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestClientResult> ResolveAsync(
        string id,
        ResolveServiceRequestInput request,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestClientResult> AddCommentAsync(
        string id,
        AddServiceRequestCommentInput request,
        CancellationToken cancellationToken = default);
}
