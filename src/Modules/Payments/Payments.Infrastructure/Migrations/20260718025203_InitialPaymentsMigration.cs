using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPaymentsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerMappings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMappings", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionStates",
                columns: table => new
                {
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CurrentPeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentMethodBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentMethodLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionStates", x => x.StripeCustomerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMappings_StripeCustomerId",
                table: "CustomerMappings",
                column: "StripeCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionStates_Status",
                table: "SubscriptionStates",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerMappings");

            migrationBuilder.DropTable(
                name: "SubscriptionStates");
        }
    }
}
