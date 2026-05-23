using OpsLedger.Core.Common.Models;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Dto;
using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Core.ServiceRequests;

public static class ServiceRequestWorkflow
{
    public static OperationResult<ServiceRequest> Assign(
        ServiceRequest request,
        AssignServiceRequestCommand command,
        DateTimeOffset changedAt)
    {
        if (string.IsNullOrWhiteSpace(command.AssigneeName))
        {
            return OperationResult<ServiceRequest>.Failure(["Assignee name is required."]);
        }

        string assigneeName = command.AssigneeName.Trim();

        ServiceRequest updated = Copy(
            request,
            RequestStatus.InProgress,
            assigneeName,
            request.ResolutionNotes,
            AppendActivity(
                request,
                new RequestActivity(
                    RequestActivityType.Assigned,
                    changedAt,
                    $"Assigned to {assigneeName}.")));

        return OperationResult<ServiceRequest>.Success(updated);
    }

    public static OperationResult<ServiceRequest> Resolve(
        ServiceRequest request,
        ResolveServiceRequestCommand command,
        DateTimeOffset changedAt)
    {
        if (string.IsNullOrWhiteSpace(command.ResolutionNotes))
        {
            return OperationResult<ServiceRequest>.Failure(["Resolution notes are required."]);
        }

        string resolutionNotes = command.ResolutionNotes.Trim();

        ServiceRequest updated = Copy(
            request,
            RequestStatus.Resolved,
            request.AssigneeName,
            resolutionNotes,
            AppendActivity(
                request,
                new RequestActivity(
                    RequestActivityType.Resolved,
                    changedAt,
                    "Request resolved.")));

        return OperationResult<ServiceRequest>.Success(updated);
    }

    private static ServiceRequest Copy(
        ServiceRequest request,
        RequestStatus status,
        string? assigneeName,
        string? resolutionNotes,
        IReadOnlyList<RequestActivity> activity)
    {
        return new ServiceRequest(
            request.Title,
            request.Category,
            request.Priority,
            request.Description,
            request.RequesterName,
            request.RequesterEmail,
            request.ImpactDetails,
            status,
            request.CreatedAt,
            request.SlaDueAt,
            activity,
            assigneeName,
            resolutionNotes);
    }

    private static IReadOnlyList<RequestActivity> AppendActivity(ServiceRequest request, RequestActivity activity)
    {
        List<RequestActivity> activities = [.. request.Activity, activity];
        return activities;
    }
}
