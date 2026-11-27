using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseBoard.Monitoring.Infrastructure.Outbox;

namespace PulseBoard.Monitoring.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityAlwaysColumn();

        builder.Property(m => m.Type).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(OutboxMessage.ErrorMaxLength);

        // Partial index: the processor only ever looks at unprocessed messages.
        builder.HasIndex(m => m.Id)
            .HasDatabaseName("IX_OutboxMessages_Unprocessed")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
