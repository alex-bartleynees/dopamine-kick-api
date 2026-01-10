using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habits.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleRemindersForHabit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HabitReminders_HabitId",
                table: "HabitReminders");

            migrationBuilder.CreateIndex(
                name: "IX_HabitReminders_HabitId",
                table: "HabitReminders",
                column: "HabitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HabitReminders_HabitId",
                table: "HabitReminders");

            migrationBuilder.CreateIndex(
                name: "IX_HabitReminders_HabitId",
                table: "HabitReminders",
                column: "HabitId",
                unique: true);
        }
    }
}
