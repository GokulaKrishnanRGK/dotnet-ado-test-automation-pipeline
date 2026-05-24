using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsLedger.Infrastructure.Database.Entities;

namespace OpsLedger.Infrastructure.Database.Configuration;

internal sealed class RequestCommentConfiguration : IEntityTypeConfiguration<RequestCommentRecord>
{
    public void Configure(EntityTypeBuilder<RequestCommentRecord> builder)
    {
        builder.ToTable("service_request_comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(c => c.AuthorName).HasColumnName("author_name").IsRequired();
        builder.Property(c => c.Body).HasColumnName("body").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
    }
}
