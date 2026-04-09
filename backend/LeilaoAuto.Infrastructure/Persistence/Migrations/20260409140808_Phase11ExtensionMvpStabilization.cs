using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeilaoAuto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11ExtensionMvpStabilization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "connector_execution_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE connector_execution_logs
                SET user_id = NULLIF((payload_json::jsonb ->> 'userId'), '')::uuid
                WHERE connector_name = 'ScannerManual'
                  AND payload_json IS NOT NULL
                  AND payload_json LIKE '%"userId"%';
                """);

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    search = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    min_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: true),
                    region = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    advanced_filters_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_connector_execution_logs_user_id_executed_at",
                table: "connector_execution_logs",
                columns: new[] { "user_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_updated_at",
                table: "user_settings",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropIndex(
                name: "IX_connector_execution_logs_user_id_executed_at",
                table: "connector_execution_logs");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "connector_execution_logs");
        }
    }
}
