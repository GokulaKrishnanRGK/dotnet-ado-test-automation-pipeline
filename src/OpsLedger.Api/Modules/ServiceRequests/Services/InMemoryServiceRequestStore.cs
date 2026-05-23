using OpsLedger.Api.Modules.ServiceRequests.Dto;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public sealed class InMemoryServiceRequestStore : IServiceRequestStore
{
    private readonly Lock _sync = new();
    private readonly List<ServiceRequestApiResponse> _requests = [];

    public void Add(ServiceRequestApiResponse request)
    {
        lock (_sync)
        {
            _requests.Add(request);
        }
    }

    public IReadOnlyList<ServiceRequestApiResponse> List(string? status, string? priority)
    {
        lock (_sync)
        {
            return _requests
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
