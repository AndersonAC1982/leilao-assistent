using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .IsRequired();

        builder.Property(user => user.Plan)
            .HasColumnName("plan")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasMany(user => user.MonitoredVehicles)
            .WithOne(vehicle => vehicle.User)
            .HasForeignKey(vehicle => vehicle.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Subscriptions)
            .WithOne(subscription => subscription.User)
            .HasForeignKey(subscription => subscription.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
