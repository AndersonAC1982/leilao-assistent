using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).HasColumnName("id");

        builder.Property(subscription => subscription.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(subscription => subscription.Provider)
            .HasColumnName("provider")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(subscription => subscription.ExternalCustomerId)
            .HasColumnName("external_customer_id")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(subscription => subscription.ExternalSubscriptionId)
            .HasColumnName("external_subscription_id")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(subscription => subscription.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(subscription => subscription.Plan)
            .HasColumnName("plan")
            .IsRequired();

        builder.Property(subscription => subscription.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(subscription => subscription.EndsAt)
            .HasColumnName("ends_at");

        builder.HasIndex(subscription => subscription.UserId);
        builder.HasIndex(subscription => subscription.ExternalSubscriptionId).IsUnique();
    }
}
