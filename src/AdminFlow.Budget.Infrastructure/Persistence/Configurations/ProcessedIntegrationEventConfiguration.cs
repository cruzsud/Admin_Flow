using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminFlow.Budget.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedIntegrationEventConfiguration
    : IEntityTypeConfiguration<ProcessedIntegrationEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedIntegrationEvent> builder)
    {
        builder.ToTable("processed_integration_events");

        builder.HasKey(processedEvent => processedEvent.EventId)
            .HasName("pk_processed_integration_events");

        builder.Property(processedEvent => processedEvent.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();

        builder.Property(processedEvent => processedEvent.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(processedEvent => processedEvent.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
