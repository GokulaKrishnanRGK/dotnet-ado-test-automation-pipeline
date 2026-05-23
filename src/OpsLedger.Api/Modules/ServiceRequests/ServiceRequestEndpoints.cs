using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Services;
using OpsLedger.Core.Common.Models;
using OpsLedger.Core.ServiceRequests;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Dto;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Api.Modules.ServiceRequests;

public static class ServiceRequestEndpoints
{
    public static IEndpointRouteBuilder MapServiceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/service-requests", ListServiceRequests)
            .WithName("ListServiceRequests");

        endpoints.MapPost("/service-requests", CreateServiceRequest)
            .WithName("CreateServiceRequest");

        return endpoints;
    }

    private static IResult ListServiceRequests(
        IServiceRequestStore store,
        string? status,
        string? priority)
    {
        IReadOnlyList<ServiceRequestApiResponse> requests = store.List(status, priority);
        return Results.Ok(requests);
    }

    private static IResult CreateServiceRequest(
        CreateServiceRequestApiRequest request,
        IServiceRequestStore store)
    {
        List<string> errors = new();

        if (!Enum.TryParse<RequestCategory>(request.Category, ignoreCase: true, out RequestCategory category))
        {
            errors.Add("Category is invalid.");
        }

        if (!Enum.TryParse<RequestPriority>(request.Priority, ignoreCase: true, out RequestPriority priority))
        {
            errors.Add("Priority is invalid.");
        }

        if (errors.Count > 0)
        {
            return Results.BadRequest(new ValidationErrorResponse(errors));
        }

        CreateServiceRequestCommand command = new(
            request.Title,
            category,
            priority,
            request.Description,
            request.RequesterName,
            request.RequesterEmail,
            request.ImpactDetails);

        OperationResult<ServiceRequest> result = ServiceRequestFactory.Create(command, DateTimeOffset.UtcNow);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ValidationErrorResponse(result.Errors));
        }

        ServiceRequestApiResponse response = ServiceRequestApiResponse.From(result.Value);
        store.Add(response);

        return Results.Created($"/service-requests/{response.Id}", response);
    }
}
