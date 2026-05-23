using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Services;

public interface IServiceRequestStore
{
    StoredServiceRequest Add(ServiceRequest request);

    StoredServiceRequest? Get(string id);

    StoredServiceRequest Update(string id, ServiceRequest request);

    IReadOnlyList<ServiceRequestApiResponse> List(string? status, string? priority);
}
