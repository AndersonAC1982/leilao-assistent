using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class ConnectorExecutionLogConfiguration : IEntityTypeConfiguration<ConnectorExecutionLog>
{
    public void Configure(EntityTypeBuilder<ConnectorExecutionLog> builder)
    {
        builder.ToTable("connector_execution_logs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasColumnName("id");

        builder.Property(log => log.ConnectorName)
            .HasColumnName("connector_name")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(log => log.ExecutedAt)
            .HasColumnName("executed_at")
            .IsRequired();

        builder.Property(log => log.Success)
            .HasColumnName("success")
            .IsRequired();

        builder.Property(log => log.RecordsRead)
            .HasColumnName("records_read")
            .IsRequired();

        builder.Property(log => log.RecordsSaved)
            .HasColumnName("records_saved")
            .IsRequired();

        builder.Property(log => log.Message)
            .HasColumnName("message")
            .HasMaxLength(1200);

        builder.Property(log => log.PayloadJson)
            .HasColumnName("payload_json");

        builder.HasIndex(log => log.ConnectorName);
        builder.HasIndex(log => log.ExecutedAt);
        builder.HasIndex(log => new { log.ConnectorName, log.ExecutedAt });
    }
}
