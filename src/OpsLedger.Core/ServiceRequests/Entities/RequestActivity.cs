using OpsLedger.Core.ServiceRequests.Constants;

namespace OpsLedger.Core.ServiceRequests.Entities;

public sealed record RequestActivity(
    RequestActivityType Type,
    DateTimeOffset OccurredAt,
    string Description);
