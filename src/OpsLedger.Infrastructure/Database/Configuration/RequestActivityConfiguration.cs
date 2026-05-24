using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsLedger.Infrastructure.Database.Entities;

namespace OpsLedger.Infrastructure.Database.Configuration;

internal sealed class RequestActivityConfiguration : IEntityTypeConfiguration<RequestActivityRecord>
{
    public void Configure(EntityTypeBuilder<RequestActivityRecord> builder)
    {
        builder.ToTable("service_request_activity");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").IsRequired();
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at");
        builder.Property(a => a.Description).HasColumnName("description").IsRequired();
    }
}
