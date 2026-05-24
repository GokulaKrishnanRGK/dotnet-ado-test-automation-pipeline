using Microsoft.EntityFrameworkCore;
using OpsLedger.Core.ServiceRequests.Constants;
using OpsLedger.Core.ServiceRequests.Entities;
using OpsLedger.Infrastructure.Persistence.Entities;

namespace OpsLedger.Infrastructure.Persistence.Repositories;

public sealed class PostgreSqlServiceRequestRepository(OpsLedgerDbContext dbContext)
{
    public async Task<PersistedServiceRequest> AddAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        string id = Guid.NewGuid().ToString("N");

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
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
             """,
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The created service request could not be loaded.");
    }

    public async Task<PersistedServiceRequest?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ServiceRequestRecord? record = await QueryRequests()
            .SingleOrDefaultAsync(
                request => request.Id == id,
                cancellationToken);

        return record is null ? null : ToPersisted(record);
    }

    public async Task<IReadOnlyList<PersistedServiceRequest>> ListAsync(
        string? status,
        string? priority,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ServiceRequestRecord> query = QueryRequests();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(request => request.Status == status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
        {
            query = query.Where(request => request.Priority == priority.Trim());
        }

        List<ServiceRequestRecord> records = await query
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(ToPersisted).ToArray();
    }

    public async Task<PersistedServiceRequest> AssignAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestActivity activity = request.Activity.Last(item => item.Type == RequestActivityType.Assigned);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT opsledger_assign_service_request(
                 {id},
                 {request.AssigneeName},
                 {activity.OccurredAt})
             """,
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The assigned service request could not be loaded.");
    }

    public async Task<PersistedServiceRequest> ResolveAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestActivity activity = request.Activity.Last(item => item.Type == RequestActivityType.Resolved);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT opsledger_resolve_service_request(
                 {id},
                 {request.ResolutionNotes},
                 {activity.OccurredAt})
             """,
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The resolved service request could not be loaded.");
    }

    public async Task<PersistedServiceRequest> AddCommentAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestComment comment = request.Comments.Last();

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT opsledger_add_service_request_comment(
                 {id},
                 {comment.AuthorName},
                 {comment.Body},
                 {comment.CreatedAt})
             """,
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The commented service request could not be loaded.");
    }

    private IQueryable<ServiceRequestRecord> QueryRequests()
    {
        return dbContext.ServiceRequests
            .AsNoTracking()
            .Include(request => request.Activity)
            .Include(request => request.Comments);
    }

    private static PersistedServiceRequest ToPersisted(ServiceRequestRecord record)
    {
        ServiceRequest request = new(
            record.Title,
            Enum.Parse<RequestCategory>(record.Category),
            Enum.Parse<RequestPriority>(record.Priority),
            record.Description,
            record.RequesterName,
            record.RequesterEmail,
            record.ImpactDetails,
            Enum.Parse<RequestStatus>(record.Status),
            record.CreatedAt,
            record.SlaDueAt,
            record.Activity
                .OrderBy(activity => activity.OccurredAt)
                .ThenBy(activity => activity.Id)
                .Select(activity => new RequestActivity(
                    Enum.Parse<RequestActivityType>(activity.Type),
                    activity.OccurredAt,
                    activity.Description))
                .ToArray(),
            record.Comments
                .OrderBy(comment => comment.CreatedAt)
                .ThenBy(comment => comment.Id)
                .Select(comment => new RequestComment(
                    comment.AuthorName,
                    comment.Body,
                    comment.CreatedAt))
                .ToArray(),
            record.AssigneeName,
            record.ResolutionNotes);

        return new PersistedServiceRequest(record.Id, request);
    }
}
