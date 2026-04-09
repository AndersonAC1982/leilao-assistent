using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LeilaoAuto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LeilaoAutoDbContext))]
    [Migration("20260409193000_Phase12GeneralAuctionSettings")]
    public partial class Phase12GeneralAuctionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "active_sources",
                table: "user_settings",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "user_settings",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "Todas");

            migrationBuilder.AddColumn<decimal>(
                name: "max_price",
                table: "user_settings",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_sources",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "category",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "max_price",
                table: "user_settings");
        }
    }
}
