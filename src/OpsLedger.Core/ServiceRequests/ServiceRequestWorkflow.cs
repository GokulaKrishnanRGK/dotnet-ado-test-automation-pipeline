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

    public static OperationResult<ServiceRequest> AddComment(
        ServiceRequest request,
        AddServiceRequestCommentCommand command,
        DateTimeOffset changedAt)
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(command.AuthorName))
        {
            errors.Add("Comment author is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Body))
        {
            errors.Add("Comment body is required.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<ServiceRequest>.Failure(errors);
        }

        string authorName = command.AuthorName.Trim();

        ServiceRequest updated = Copy(
            request,
            request.Status,
            request.AssigneeName,
            request.ResolutionNotes,
            AppendActivity(
                request,
                new RequestActivity(
                    RequestActivityType.CommentAdded,
                    changedAt,
                    $"Comment added by {authorName}.")),
            AppendComment(
                request,
                new RequestComment(
                    authorName,
                    command.Body.Trim(),
                    changedAt)));

        return OperationResult<ServiceRequest>.Success(updated);
    }

    private static ServiceRequest Copy(
        ServiceRequest request,
        RequestStatus status,
        string? assigneeName,
        string? resolutionNotes,
        IReadOnlyList<RequestActivity> activity,
        IReadOnlyList<RequestComment>? comments = null)
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
            comments ?? request.Comments,
            assigneeName,
            resolutionNotes);
    }

    private static IReadOnlyList<RequestActivity> AppendActivity(ServiceRequest request, RequestActivity activity)
    {
        List<RequestActivity> activities = [.. request.Activity, activity];
        return activities;
    }

    private static IReadOnlyList<RequestComment> AppendComment(ServiceRequest request, RequestComment comment)
    {
        List<RequestComment> comments = [.. request.Comments, comment];
        return comments;
    }
}
