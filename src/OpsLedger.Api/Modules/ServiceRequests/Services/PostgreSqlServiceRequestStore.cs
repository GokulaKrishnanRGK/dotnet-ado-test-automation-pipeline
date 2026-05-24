using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
using OpsLedger.Core.ServiceRequests.Entities;
using OpsLedger.Infrastructure.Persistence.Repositories;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public sealed class PostgreSqlServiceRequestStore(PostgreSqlServiceRequestRepository repository) : IServiceRequestStore
{
    public async Task<StoredServiceRequest> AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        PersistedServiceRequest persisted = await repository.AddAsync(request, cancellationToken);
        return ToStored(persisted);
    }

    public async Task<StoredServiceRequest?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        PersistedServiceRequest? persisted = await repository.GetAsync(id, cancellationToken);
        return persisted is null ? null : ToStored(persisted);
    }

    public async Task<StoredServiceRequest> AssignAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        PersistedServiceRequest persisted = await repository.AssignAsync(id, request, cancellationToken);
        return ToStored(persisted);
    }

    public async Task<StoredServiceRequest> ResolveAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        PersistedServiceRequest persisted = await repository.ResolveAsync(id, request, cancellationToken);
        return ToStored(persisted);
    }

    public async Task<StoredServiceRequest> AddCommentAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        PersistedServiceRequest persisted = await repository.AddCommentAsync(id, request, cancellationToken);
        return ToStored(persisted);
    }

    public async Task<IReadOnlyList<ServiceRequestApiResponse>> ListAsync(
        string? status,
        string? priority,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PersistedServiceRequest> requests = await repository.ListAsync(status, priority, cancellationToken);
        return requests.Select(request => ServiceRequestApiResponse.From(request.Id, request.Request)).ToArray();
    }

    private static StoredServiceRequest ToStored(PersistedServiceRequest persisted)
    {
        return new StoredServiceRequest(persisted.Id, persisted.Request);
    }
}
