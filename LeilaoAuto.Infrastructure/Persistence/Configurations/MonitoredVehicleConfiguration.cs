using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class MonitoredVehicleConfiguration : IEntityTypeConfiguration<MonitoredVehicle>
{
    public void Configure(EntityTypeBuilder<MonitoredVehicle> builder)
    {
        builder.ToTable("monitored_vehicles");

        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasColumnName("id");

        builder.Property(vehicle => vehicle.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(vehicle => vehicle.Make)
            .HasColumnName("make")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(vehicle => vehicle.Model)
            .HasColumnName("model")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(vehicle => vehicle.NormalizedModel)
            .HasColumnName("normalized_model")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(vehicle => vehicle.YearFrom).HasColumnName("year_from");
        builder.Property(vehicle => vehicle.YearTo).HasColumnName("year_to");
        builder.Property(vehicle => vehicle.VehicleType).HasColumnName("vehicle_type");
        builder.Property(vehicle => vehicle.Uf).HasColumnName("uf").HasMaxLength(2);
        builder.Property(vehicle => vehicle.VehicleCondition).HasColumnName("vehicle_condition");

        builder.Property(vehicle => vehicle.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(vehicle => new { vehicle.UserId, vehicle.NormalizedModel });
    }
}
