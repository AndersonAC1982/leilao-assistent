using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).HasColumnName("id");

        builder.Property(settings => settings.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(settings => settings.Search)
            .HasColumnName("search")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(settings => settings.Source)
            .HasColumnName("source")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(settings => settings.MinScore)
            .HasColumnName("min_score")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(settings => settings.VehicleType)
            .HasColumnName("vehicle_type");

        builder.Property(settings => settings.Region)
            .HasColumnName("region")
            .HasMaxLength(10);

        builder.Property(settings => settings.AdvancedFiltersEnabled)
            .HasColumnName("advanced_filters_enabled")
            .IsRequired();

        builder.Property(settings => settings.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(settings => settings.UserId).IsUnique();
        builder.HasIndex(settings => settings.UpdatedAt);
    }
}
