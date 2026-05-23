using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public sealed class InMemoryServiceRequestStore : IServiceRequestStore
{
    private readonly Lock _sync = new();
    private readonly List<StoredServiceRequest> _requests = [];

    public StoredServiceRequest Add(ServiceRequest request)
    {
        lock (_sync)
        {
            StoredServiceRequest storedRequest = new(Guid.NewGuid().ToString("N"), request);
            _requests.Add(storedRequest);
            return storedRequest;
        }
    }

    public StoredServiceRequest? Get(string id)
    {
        lock (_sync)
        {
            return _requests.SingleOrDefault(request =>
                string.Equals(request.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public StoredServiceRequest Update(string id, ServiceRequest request)
    {
        lock (_sync)
        {
            Int32 index = _requests.FindIndex(storedRequest =>
                string.Equals(storedRequest.Id, id, StringComparison.OrdinalIgnoreCase));

            StoredServiceRequest updated = new(id, request);
            _requests[index] = updated;
            return updated;
        }
    }

    public IReadOnlyList<ServiceRequestApiResponse> List(string? status, string? priority)
    {
        lock (_sync)
        {
            return _requests
                .Select(request => ServiceRequestApiResponse.From(request.Id, request.Request))
                .Where(request => Matches(request.Status, status))
                .Where(request => Matches(request.Priority, priority))
                .OrderByDescending(request => request.CreatedAt)
                .ToArray();
        }
    }

    private static bool Matches(string actual, string? expected)
    {
        return string.IsNullOrWhiteSpace(expected) ||
            string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
