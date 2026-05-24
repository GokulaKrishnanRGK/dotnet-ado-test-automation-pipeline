using OpsLedger.Api.Modules.ServiceRequests.Dto;
using OpsLedger.Api.Modules.ServiceRequests.Models;
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

        endpoints.MapGet("/service-requests/{id}", GetServiceRequest)
            .WithName("GetServiceRequest");

        endpoints.MapPost("/service-requests", CreateServiceRequest)
            .WithName("CreateServiceRequest");

        endpoints.MapPatch("/service-requests/{id}/assignment", AssignServiceRequest)
            .WithName("AssignServiceRequest");

        endpoints.MapPatch("/service-requests/{id}/resolution", ResolveServiceRequest)
            .WithName("ResolveServiceRequest");

        endpoints.MapPost("/service-requests/{id}/comments", AddServiceRequestComment)
            .WithName("AddServiceRequestComment");

        return endpoints;
    }

    private static async Task<IResult> ListServiceRequests(
        IServiceRequestStore store,
        string? status,
        string? priority,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ServiceRequestApiResponse> requests = await store.ListAsync(status, priority, cancellationToken);
        return Results.Ok(requests);
    }

    private static async Task<IResult> GetServiceRequest(
        string id,
        IServiceRequestStore store,
        CancellationToken cancellationToken)
    {
        StoredServiceRequest? storedRequest = await store.GetAsync(id, cancellationToken);

        if (storedRequest is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ServiceRequestApiResponse.From(storedRequest.Id, storedRequest.Request));
    }

    private static async Task<IResult> CreateServiceRequest(
        CreateServiceRequestApiRequest request,
        IServiceRequestStore store,
        CancellationToken cancellationToken)
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

        StoredServiceRequest storedRequest = await store.AddAsync(result.Value, cancellationToken);
        ServiceRequestApiResponse response = ServiceRequestApiResponse.From(storedRequest.Id, storedRequest.Request);

        return Results.Created($"/service-requests/{response.Id}", response);
    }

    private static async Task<IResult> AssignServiceRequest(
        string id,
        AssignServiceRequestApiRequest request,
        IServiceRequestStore store,
        CancellationToken cancellationToken)
    {
        StoredServiceRequest? storedRequest = await store.GetAsync(id, cancellationToken);

        if (storedRequest is null)
        {
            return Results.NotFound();
        }

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Assign(
            storedRequest.Request,
            new AssignServiceRequestCommand(request.AssigneeName),
            DateTimeOffset.UtcNow);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ValidationErrorResponse(result.Errors));
        }

        StoredServiceRequest updated = await store.AssignAsync(storedRequest.Id, result.Value, cancellationToken);
        return Results.Ok(ServiceRequestApiResponse.From(updated.Id, updated.Request));
    }

    private static async Task<IResult> ResolveServiceRequest(
        string id,
        ResolveServiceRequestApiRequest request,
        IServiceRequestStore store,
        CancellationToken cancellationToken)
    {
        StoredServiceRequest? storedRequest = await store.GetAsync(id, cancellationToken);

        if (storedRequest is null)
        {
            return Results.NotFound();
        }

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.Resolve(
            storedRequest.Request,
            new ResolveServiceRequestCommand(request.ResolutionNotes),
            DateTimeOffset.UtcNow);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ValidationErrorResponse(result.Errors));
        }

        StoredServiceRequest updated = await store.ResolveAsync(storedRequest.Id, result.Value, cancellationToken);
        return Results.Ok(ServiceRequestApiResponse.From(updated.Id, updated.Request));
    }

    private static async Task<IResult> AddServiceRequestComment(
        string id,
        AddServiceRequestCommentApiRequest request,
        IServiceRequestStore store,
        CancellationToken cancellationToken)
    {
        StoredServiceRequest? storedRequest = await store.GetAsync(id, cancellationToken);

        if (storedRequest is null)
        {
            return Results.NotFound();
        }

        OperationResult<ServiceRequest> result = ServiceRequestWorkflow.AddComment(
            storedRequest.Request,
            new AddServiceRequestCommentCommand(request.AuthorName, request.Body),
            DateTimeOffset.UtcNow);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new ValidationErrorResponse(result.Errors));
        }

        StoredServiceRequest updated = await store.AddCommentAsync(storedRequest.Id, result.Value, cancellationToken);
        return Results.Ok(ServiceRequestApiResponse.From(updated.Id, updated.Request));
    }
}
