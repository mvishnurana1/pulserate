using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Infrastructure.Persistence.Configurations;

internal sealed class CheckResultConfiguration : IEntityTypeConfiguration<CheckResult>
{
    public void Configure(EntityTypeBuilder<CheckResult> builder)
    {
        builder.ToTable("CheckResults");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Error).HasMaxLength(CheckResult.ErrorMaxLength);

        builder.HasOne<Check>()
            .WithMany()
            .HasForeignKey(r => r.CheckId)
            .OnDelete(DeleteBehavior.Cascade);

        // Matches the history query: newest results for one check.
        builder.HasIndex(r => new { r.CheckId, r.CheckedAt })
            .IsDescending(false, true);
    }
}
