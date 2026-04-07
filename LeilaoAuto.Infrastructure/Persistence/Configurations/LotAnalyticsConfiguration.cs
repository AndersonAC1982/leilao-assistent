using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class LotAnalyticsConfiguration : IEntityTypeConfiguration<LotAnalytics>
{
    public void Configure(EntityTypeBuilder<LotAnalytics> builder)
    {
        builder.ToTable("lot_analytics");

        builder.HasKey(analytics => analytics.Id);
        builder.Property(analytics => analytics.Id).HasColumnName("id");

        builder.Property(analytics => analytics.NormalizedModel)
            .HasColumnName("normalized_model")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(analytics => analytics.AveragePrice)
            .HasColumnName("average_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(analytics => analytics.MinPrice)
            .HasColumnName("min_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(analytics => analytics.MaxPrice)
            .HasColumnName("max_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(analytics => analytics.SampleSize)
            .HasColumnName("sample_size")
            .IsRequired();

        builder.Property(analytics => analytics.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(analytics => analytics.NormalizedModel).IsUnique();
    }
}
