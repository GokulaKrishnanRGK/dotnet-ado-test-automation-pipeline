using OpsLedger.Api.Modules.ServiceRequests.Dto;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public interface IServiceRequestStore
{
    void Add(ServiceRequestApiResponse request);

    IReadOnlyList<ServiceRequestApiResponse> List(string? status, string? priority);
}
