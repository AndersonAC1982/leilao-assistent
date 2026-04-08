using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class AuctionLotConfiguration : IEntityTypeConfiguration<AuctionLot>
{
    public void Configure(EntityTypeBuilder<AuctionLot> builder)
    {
        builder.ToTable("auction_lots");

        builder.HasKey(lot => lot.Id);
        builder.Property(lot => lot.Id).HasColumnName("id");

        builder.Property(lot => lot.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(lot => lot.Auctioneer)
            .HasColumnName("auctioneer")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(lot => lot.LotNumber)
            .HasColumnName("lot_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(lot => lot.Make)
            .HasColumnName("make")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(lot => lot.Model)
            .HasColumnName("model")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(lot => lot.NormalizedModel)
            .HasColumnName("normalized_model")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(lot => lot.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(lot => lot.VehicleType)
            .HasColumnName("vehicle_type")
            .IsRequired();

        builder.Property(lot => lot.Uf)
            .HasColumnName("uf")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(lot => lot.VehicleCondition)
            .HasColumnName("vehicle_condition")
            .IsRequired();

        builder.Property(lot => lot.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(lot => lot.CurrentBid)
            .HasColumnName("current_bid")
            .HasPrecision(14, 2);

        builder.Property(lot => lot.FinalPrice)
            .HasColumnName("final_price")
            .HasPrecision(14, 2);

        builder.Property(lot => lot.AppraisedValue)
            .HasColumnName("appraised_value")
            .HasPrecision(14, 2);

        builder.Property(lot => lot.LotUrl)
            .HasColumnName("lot_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(lot => lot.StartsAt).HasColumnName("starts_at");
        builder.Property(lot => lot.EndsAt).HasColumnName("ends_at");

        builder.Property(lot => lot.IsProcessed)
            .HasColumnName("is_processed")
            .IsRequired();

        builder.Property(lot => lot.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(lot => lot.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(lot => lot.ExternalId).IsUnique();
        builder.HasIndex(lot => new { lot.Status, lot.NormalizedModel });
        builder.HasIndex(lot => new { lot.IsProcessed, lot.Status });
        builder.HasIndex(lot => new { lot.Auctioneer, lot.LotNumber, lot.Status });
    }
}
