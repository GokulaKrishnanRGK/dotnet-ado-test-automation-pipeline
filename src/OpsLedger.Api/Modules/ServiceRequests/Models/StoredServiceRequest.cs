using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Models;

public sealed record StoredServiceRequest(string Id, ServiceRequest Request);
