using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habits.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHabitsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HabitReminder_Habits_HabitId",
                table: "HabitReminder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HabitReminder",
                table: "HabitReminder");

            migrationBuilder.RenameTable(
                name: "HabitReminder",
                newName: "HabitReminders");

            migrationBuilder.RenameIndex(
                name: "IX_HabitReminder_UserId",
                table: "HabitReminders",
                newName: "IX_HabitReminders_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_HabitReminder_HabitId",
                table: "HabitReminders",
                newName: "IX_HabitReminders_HabitId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HabitReminders",
                table: "HabitReminders",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitReminders_Habits_HabitId",
                table: "HabitReminders",
                column: "HabitId",
                principalTable: "Habits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HabitReminders_Habits_HabitId",
                table: "HabitReminders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HabitReminders",
                table: "HabitReminders");

            migrationBuilder.RenameTable(
                name: "HabitReminders",
                newName: "HabitReminder");

            migrationBuilder.RenameIndex(
                name: "IX_HabitReminders_UserId",
                table: "HabitReminder",
                newName: "IX_HabitReminder_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_HabitReminders_HabitId",
                table: "HabitReminder",
                newName: "IX_HabitReminder_HabitId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HabitReminder",
                table: "HabitReminder",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitReminder_Habits_HabitId",
                table: "HabitReminder",
                column: "HabitId",
                principalTable: "Habits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
