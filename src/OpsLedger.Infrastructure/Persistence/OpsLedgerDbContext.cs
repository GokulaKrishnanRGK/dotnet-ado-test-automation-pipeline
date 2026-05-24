using Microsoft.EntityFrameworkCore;
using OpsLedger.Infrastructure.Persistence.Entities;

namespace OpsLedger.Infrastructure.Persistence;

public sealed class OpsLedgerDbContext : DbContext
{
    public OpsLedgerDbContext(DbContextOptions<OpsLedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceRequestRecord> ServiceRequests => Set<ServiceRequestRecord>();
    public DbSet<RequestActivityRecord> RequestActivity => Set<RequestActivityRecord>();
    public DbSet<RequestCommentRecord> RequestComments => Set<RequestCommentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpsLedgerDbContext).Assembly);
    }
}
