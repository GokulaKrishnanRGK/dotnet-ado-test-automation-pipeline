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
    string? AssigneeName,
    string? ResolutionNotes,
    IReadOnlyList<string> Activity)
{
    public static ServiceRequestApiResponse From(ServiceRequest request)
    {
        return From(Guid.NewGuid().ToString("N"), request);
    }

    public static ServiceRequestApiResponse From(string id, ServiceRequest request)
    {
        return new ServiceRequestApiResponse(
            id,
            request.Title,
            request.Category.ToString(),
            request.Priority.ToString(),
            request.Status.ToString(),
            request.CreatedAt,
            request.SlaDueAt,
            request.AssigneeName,
            request.ResolutionNotes,
            request.Activity.Select(activity => activity.Type.ToString()).ToArray());
    }
}
