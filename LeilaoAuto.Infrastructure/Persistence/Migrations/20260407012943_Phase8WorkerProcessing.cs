using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeilaoAuto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8WorkerProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "auction_lots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at_utc",
                table: "auction_lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_auction_lots_is_processed_status",
                table: "auction_lots",
                columns: new[] { "is_processed", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_auction_lots_is_processed_status",
                table: "auction_lots");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "auction_lots");

            migrationBuilder.DropColumn(
                name: "processed_at_utc",
                table: "auction_lots");
        }
    }
}
