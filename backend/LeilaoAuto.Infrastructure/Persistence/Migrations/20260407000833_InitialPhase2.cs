using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeilaoAuto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auction_lots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    auctioneer = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    lot_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    make = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    vehicle_condition = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    lot_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    current_bid = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    final_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    appraised_value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction_lots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connector_execution_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    records_read = table.Column<int>(type: "integer", nullable: false),
                    records_saved = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connector_execution_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lot_analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    average_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    min_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    max_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    sample_size = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_analytics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_site = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    normalized_title = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    brand = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    vehicle_state = table.Column<int>(type: "integer", nullable: true),
                    lot_url = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false),
                    image_url = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                    description = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    current_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    final_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    found_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_data_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lots", x => x.id);
                    table.CheckConstraint("ck_lots_price_non_negative", "\"current_price\" IS NULL OR \"current_price\" >= 0");
                    table.CheckConstraint("ck_lots_url_not_empty", "length(trim(\"lot_url\")) > 0");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monitored_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    vehicle_state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    normalized_model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitored_vehicles", x => x.id);
                    table.CheckConstraint("ck_monitored_vehicles_year", "\"year\" >= 1960 AND \"year\" <= 2100");
                    table.ForeignKey(
                        name: "FK_monitored_vehicles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    external_customer_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    external_subscription_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_lots_auctioneer_lot_number_status",
                table: "auction_lots",
                columns: new[] { "auctioneer", "lot_number", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_auction_lots_external_id",
                table: "auction_lots",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auction_lots_status_normalized_model",
                table: "auction_lots",
                columns: new[] { "status", "normalized_model" });

            migrationBuilder.CreateIndex(
                name: "IX_connector_execution_logs_connector_name",
                table: "connector_execution_logs",
                column: "connector_name");

            migrationBuilder.CreateIndex(
                name: "IX_connector_execution_logs_connector_name_executed_at",
                table: "connector_execution_logs",
                columns: new[] { "connector_name", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_connector_execution_logs_executed_at",
                table: "connector_execution_logs",
                column: "executed_at");

            migrationBuilder.CreateIndex(
                name: "IX_lot_analytics_normalized_model",
                table: "lot_analytics",
                column: "normalized_model",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lots_brand_model_year",
                table: "lots",
                columns: new[] { "brand", "model", "year" });

            migrationBuilder.CreateIndex(
                name: "IX_lots_found_at",
                table: "lots",
                column: "found_at");

            migrationBuilder.CreateIndex(
                name: "IX_lots_lot_url",
                table: "lots",
                column: "lot_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lots_normalized_title",
                table: "lots",
                column: "normalized_title");

            migrationBuilder.CreateIndex(
                name: "IX_lots_status",
                table: "lots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_monitored_vehicles_user_id_brand_model_year_uf",
                table: "monitored_vehicles",
                columns: new[] { "user_id", "brand", "model", "year", "uf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_monitored_vehicles_user_id_created_at",
                table: "monitored_vehicles",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_monitored_vehicles_user_id_normalized_model",
                table: "monitored_vehicles",
                columns: new[] { "user_id", "normalized_model" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_external_subscription_id",
                table: "subscriptions",
                column: "external_subscription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_user_id",
                table: "subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_lots");

            migrationBuilder.DropTable(
                name: "connector_execution_logs");

            migrationBuilder.DropTable(
                name: "lot_analytics");

            migrationBuilder.DropTable(
                name: "lots");

            migrationBuilder.DropTable(
                name: "monitored_vehicles");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
