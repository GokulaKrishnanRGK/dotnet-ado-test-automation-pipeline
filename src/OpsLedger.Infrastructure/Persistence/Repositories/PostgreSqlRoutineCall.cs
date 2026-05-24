using OpsLedger.Core.ServiceRequests.Entities;

namespace OpsLedger.Infrastructure.Persistence.Repositories;

internal static class PostgreSqlRoutineCall
{
    public static FormattableString CreateServiceRequest(string id, ServiceRequest request)
    {
        return $"""
                SELECT opsledger_create_service_request(
                    {id},
                    {request.Title},
                    {request.Category.ToString()},
                    {request.Priority.ToString()},
                    {request.Description},
                    {request.RequesterName},
                    {request.RequesterEmail},
                    {request.ImpactDetails},
                    {request.Status.ToString()},
                    {request.CreatedAt},
                    {request.SlaDueAt})
                """;
    }

    public static FormattableString GetServiceRequest(string id)
    {
        return $"SELECT * FROM opsledger_get_service_request({id})";
    }

    public static FormattableString ListServiceRequests(string? status, string? priority)
    {
        return $"SELECT * FROM opsledger_list_service_requests({status}, {priority})";
    }

    public static FormattableString AssignServiceRequest(string id, string? assigneeName, DateTimeOffset changedAt)
    {
        return $"""
                SELECT opsledger_assign_service_request(
                    {id},
                    {assigneeName},
                    {changedAt})
                """;
    }

    public static FormattableString ResolveServiceRequest(string id, string? resolutionNotes, DateTimeOffset changedAt)
    {
        return $"""
                SELECT opsledger_resolve_service_request(
                    {id},
                    {resolutionNotes},
                    {changedAt})
                """;
    }

    public static FormattableString AddServiceRequestComment(string id, RequestComment comment)
    {
        return $"""
                SELECT opsledger_add_service_request_comment(
                    {id},
                    {comment.AuthorName},
                    {comment.Body},
                    {comment.CreatedAt})
                """;
    }
}
