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

        builder.Property(vehicle => vehicle.Brand)
            .HasColumnName("brand")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(vehicle => vehicle.Model)
            .HasColumnName("model")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(vehicle => vehicle.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(vehicle => vehicle.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(vehicle => vehicle.Uf)
            .HasColumnName("uf")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(vehicle => vehicle.VehicleState)
            .HasColumnName("vehicle_state")
            .IsRequired();

        builder.Property(vehicle => vehicle.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(vehicle => vehicle.NormalizedModel)
            .HasColumnName("normalized_model")
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(vehicle => new { vehicle.UserId, vehicle.CreatedAt });
        builder.HasIndex(vehicle => new { vehicle.UserId, vehicle.NormalizedModel });
        builder.HasIndex(vehicle => new { vehicle.UserId, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.Uf })
            .IsUnique();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_monitored_vehicles_year", "\"year\" >= 1960 AND \"year\" <= 2100");
        });
    }
}
