using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public sealed class InMemoryServiceRequestStore : IServiceRequestStore
{
    private readonly Lock _sync = new();
    private readonly List<StoredServiceRequest> _requests = [];

    public Task<StoredServiceRequest> AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            StoredServiceRequest storedRequest = new(Guid.NewGuid().ToString("N"), request);
            _requests.Add(storedRequest);
            return Task.FromResult(storedRequest);
        }
    }

    public Task<StoredServiceRequest?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            StoredServiceRequest? request = _requests.SingleOrDefault(request =>
                string.Equals(request.Id, id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(request);
        }
    }

    public Task<StoredServiceRequest> AssignAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync(id, request, cancellationToken);
    }

    public Task<StoredServiceRequest> ResolveAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync(id, request, cancellationToken);
    }

    public Task<StoredServiceRequest> AddCommentAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        return UpdateAsync(id, request, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceRequestApiResponse>> ListAsync(
        string? status,
        string? priority,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<ServiceRequestApiResponse> requests = _requests
                .Select(request => ServiceRequestApiResponse.From(request.Id, request.Request))
                .Where(request => Matches(request.Status, status))
                .Where(request => Matches(request.Priority, priority))
                .OrderByDescending(request => request.CreatedAt)
                .ToArray();

            return Task.FromResult(requests);
        }
    }

    private Task<StoredServiceRequest> UpdateAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            Int32 index = _requests.FindIndex(storedRequest =>
                string.Equals(storedRequest.Id, id, StringComparison.OrdinalIgnoreCase));

            StoredServiceRequest updated = new(id, request);
            _requests[index] = updated;
            return Task.FromResult(updated);
        }
    }

    private static bool Matches(string actual, string? expected)
    {
        return string.IsNullOrWhiteSpace(expected) ||
            string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
