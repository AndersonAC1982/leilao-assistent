using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeilaoAuto.Infrastructure.Persistence.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.ToTable("lots");

        builder.HasKey(lot => lot.Id);
        builder.Property(lot => lot.Id).HasColumnName("id");

        builder.Property(lot => lot.SourceSite)
            .HasColumnName("source_site")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(lot => lot.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(lot => lot.NormalizedTitle)
            .HasColumnName("normalized_title")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(lot => lot.Brand)
            .HasColumnName("brand")
            .HasMaxLength(60);

        builder.Property(lot => lot.Model)
            .HasColumnName("model")
            .HasMaxLength(100);

        builder.Property(lot => lot.Year)
            .HasColumnName("year");

        builder.Property(lot => lot.Type)
            .HasColumnName("type");

        builder.Property(lot => lot.Uf)
            .HasColumnName("uf")
            .HasMaxLength(2);

        builder.Property(lot => lot.VehicleState)
            .HasColumnName("vehicle_state");

        builder.Property(lot => lot.LotUrl)
            .HasColumnName("lot_url")
            .HasMaxLength(700)
            .IsRequired();

        builder.Property(lot => lot.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(700);

        builder.Property(lot => lot.Description)
            .HasColumnName("description")
            .HasMaxLength(6000);

        builder.Property(lot => lot.CurrentPrice)
            .HasColumnName("current_price")
            .HasPrecision(14, 2);

        builder.Property(lot => lot.FinalPrice)
            .HasColumnName("final_price")
            .HasPrecision(14, 2);

        builder.Property(lot => lot.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(lot => lot.FoundAt)
            .HasColumnName("found_at")
            .IsRequired();

        builder.Property(lot => lot.ClosedAt)
            .HasColumnName("closed_at");

        builder.Property(lot => lot.RawDataJson)
            .HasColumnName("raw_data_json");

        builder.HasIndex(lot => lot.Status);
        builder.HasIndex(lot => lot.FoundAt);
        builder.HasIndex(lot => lot.NormalizedTitle);
        builder.HasIndex(lot => new { lot.Brand, lot.Model, lot.Year });
        builder.HasIndex(lot => lot.LotUrl).IsUnique();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_lots_price_non_negative", "\"current_price\" IS NULL OR \"current_price\" >= 0");
            tableBuilder.HasCheckConstraint("ck_lots_url_not_empty", "length(trim(\"lot_url\")) > 0");
        });
    }
}
