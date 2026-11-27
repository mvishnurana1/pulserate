using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseBoard.Incidents.Application.Abstractions;
using PulseBoard.Incidents.Domain.Incidents;

namespace PulseBoard.Incidents.Infrastructure.Persistence;

/// <summary>
/// Owns the "incidents" schema. It never reads Monitoring's tables: everything it needs arrives in events.
/// </summary>
public sealed class IncidentsDbContext(DbContextOptions<IncidentsDbContext> options)
    : DbContext(options), IIncidentsDbContext
{
    public const string Schema = "incidents";

    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IncidentsDbContext).Assembly);
    }
}

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.CheckName).HasMaxLength(Incident.CheckNameMaxLength).IsRequired();
        builder.Property(i => i.Url).HasMaxLength(Incident.UrlMaxLength).IsRequired();
        builder.Property(i => i.LastError).HasMaxLength(Incident.ErrorMaxLength);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Resolution).HasConversion<string>().HasMaxLength(20);

        // At most one open incident per check, enforced by the database as well as the handler.
        builder.HasIndex(i => i.CheckId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Open'")
            .HasDatabaseName("IX_Incidents_CheckId_Open");

        builder.HasIndex(i => i.OpenedAt);

        builder.Ignore(i => i.DomainEvents);
    }
}
