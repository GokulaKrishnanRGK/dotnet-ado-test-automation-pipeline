using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Infrastructure.Persistence.Repositories;

public sealed record PersistedServiceRequest(string Id, ServiceRequest Request);
