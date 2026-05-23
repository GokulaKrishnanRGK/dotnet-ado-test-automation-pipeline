using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests.Dto;

public sealed record ServiceRequestApiResponse(
    string Id,
    string Title,
    string Category,
    string Priority,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    IReadOnlyList<string> Activity)
{
    public static ServiceRequestApiResponse From(ServiceRequest request)
    {
        return new ServiceRequestApiResponse(
            Guid.NewGuid().ToString("N"),
            request.Title,
            request.Category.ToString(),
            request.Priority.ToString(),
            request.Status.ToString(),
            request.CreatedAt,
            request.SlaDueAt,
            request.Activity.Select(activity => activity.Type.ToString()).ToArray());
    }
}
