using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameProviderReferenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Anti-corruption layer rename: provider ids are stored as opaque references named after
            // the domain concept, not the vendor. Column data is preserved (rename, not recreate).
            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "CustomerMappings",
                newName: "CustomerReference");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerMappings_StripeCustomerId",
                table: "CustomerMappings",
                newName: "IX_CustomerMappings_CustomerReference");

            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "SubscriptionStates",
                newName: "CustomerReference");

            migrationBuilder.RenameColumn(
                name: "SubscriptionId",
                table: "SubscriptionStates",
                newName: "SubscriptionReference");

            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "InboxMessages",
                newName: "CustomerReference");

            migrationBuilder.RenameColumn(
                name: "StripeEventId",
                table: "InboxMessages",
                newName: "EventReference");

            migrationBuilder.RenameIndex(
                name: "IX_InboxMessages_StripeEventId",
                table: "InboxMessages",
                newName: "IX_InboxMessages_EventReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomerReference",
                table: "CustomerMappings",
                newName: "StripeCustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerMappings_CustomerReference",
                table: "CustomerMappings",
                newName: "IX_CustomerMappings_StripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "CustomerReference",
                table: "SubscriptionStates",
                newName: "StripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "SubscriptionReference",
                table: "SubscriptionStates",
                newName: "SubscriptionId");

            migrationBuilder.RenameColumn(
                name: "CustomerReference",
                table: "InboxMessages",
                newName: "StripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "EventReference",
                table: "InboxMessages",
                newName: "StripeEventId");

            migrationBuilder.RenameIndex(
                name: "IX_InboxMessages_EventReference",
                table: "InboxMessages",
                newName: "IX_InboxMessages_StripeEventId");
        }
    }
}
