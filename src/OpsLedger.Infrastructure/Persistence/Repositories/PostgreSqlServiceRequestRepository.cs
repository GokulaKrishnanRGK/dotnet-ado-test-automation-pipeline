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

        await dbContext.Database.ExecuteSqlAsync(
            PostgreSqlRoutineCall.CreateServiceRequest(id, request),
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The created service request could not be loaded.");
    }

    public async Task<PersistedServiceRequest?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ServiceRequestRecord? record = await dbContext.ServiceRequests
            .FromSql(PostgreSqlRoutineCall.GetServiceRequest(id))
            .AsNoTracking()
            .Include(request => request.Activity)
            .Include(request => request.Comments)
            .SingleOrDefaultAsync(cancellationToken);

        return record is null ? null : ToPersisted(record);
    }

    public async Task<IReadOnlyList<PersistedServiceRequest>> ListAsync(
        string? status,
        string? priority,
        CancellationToken cancellationToken = default)
    {
        List<ServiceRequestRecord> records = await dbContext.ServiceRequests
            .FromSql(PostgreSqlRoutineCall.ListServiceRequests(status, priority))
            .AsNoTracking()
            .Include(request => request.Activity)
            .Include(request => request.Comments)
            .ToListAsync(cancellationToken);

        return records.Select(ToPersisted).ToArray();
    }

    public async Task<PersistedServiceRequest> AssignAsync(
        string id,
        ServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        RequestActivity activity = request.Activity.Last(item => item.Type == RequestActivityType.Assigned);

        await dbContext.Database.ExecuteSqlAsync(
            PostgreSqlRoutineCall.AssignServiceRequest(id, request.AssigneeName, activity.OccurredAt),
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

        await dbContext.Database.ExecuteSqlAsync(
            PostgreSqlRoutineCall.ResolveServiceRequest(id, request.ResolutionNotes, activity.OccurredAt),
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

        await dbContext.Database.ExecuteSqlAsync(
            PostgreSqlRoutineCall.AddServiceRequestComment(id, comment),
            cancellationToken);

        PersistedServiceRequest? persisted = await GetAsync(id, cancellationToken);
        return persisted ?? throw new InvalidOperationException("The commented service request could not be loaded.");
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
