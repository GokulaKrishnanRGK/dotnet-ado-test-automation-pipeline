using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public interface IServiceRequestStore
{
    Task<StoredServiceRequest> AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<StoredServiceRequest?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<StoredServiceRequest> AssignAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<StoredServiceRequest> ResolveAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<StoredServiceRequest> AddCommentAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestApiResponse>> ListAsync(
        string? status,
        string? priority,
        CancellationToken cancellationToken = default);
}
