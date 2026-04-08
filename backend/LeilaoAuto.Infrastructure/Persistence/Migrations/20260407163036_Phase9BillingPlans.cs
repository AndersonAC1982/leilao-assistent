using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeilaoAuto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9BillingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backward compatibility:
            // In previous phases, value 3 represented Enterprise.
            // In Phase 9, value 3 is Premium and Elite moved to 4.
            migrationBuilder.Sql("""
                UPDATE users
                SET plan = 4
                WHERE plan = 3;
                """);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET plan = 4
                WHERE plan = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET plan = 3
                WHERE plan = 4;
                """);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET plan = 3
                WHERE plan = 4;
                """);
        }
    }
}
