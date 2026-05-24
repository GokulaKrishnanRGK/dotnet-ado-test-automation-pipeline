using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsLedger.Infrastructure.Database.Entities;

namespace OpsLedger.Infrastructure.Database.Configuration;

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequestRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRequestRecord> builder)
    {
        builder.ToTable("service_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Title).HasColumnName("title").IsRequired();
        builder.Property(r => r.Category).HasColumnName("category").IsRequired();
        builder.Property(r => r.Priority).HasColumnName("priority").IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").IsRequired();
        builder.Property(r => r.RequesterName).HasColumnName("requester_name").IsRequired();
        builder.Property(r => r.RequesterEmail).HasColumnName("requester_email").IsRequired();
        builder.Property(r => r.ImpactDetails).HasColumnName("impact_details");
        builder.Property(r => r.Status).HasColumnName("status").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.SlaDueAt).HasColumnName("sla_due_at");
        builder.Property(r => r.AssigneeName).HasColumnName("assignee_name");
        builder.Property(r => r.ResolutionNotes).HasColumnName("resolution_notes");

        builder.HasMany(r => r.Activity)
            .WithOne()
            .HasForeignKey(a => a.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Comments)
            .WithOne()
            .HasForeignKey(c => c.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
