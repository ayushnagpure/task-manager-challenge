using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssignedTo", "CreatedDate", "Description", "IsDeleted", "ModifiedDate", "Priority", "Status", "Title" },
                values: new object[,]
                {
                    { 1, "Bob", new DateTimeOffset(new DateTime(2026, 1, 2, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Move 2025 project folders to cold storage.", false, new DateTimeOffset(new DateTime(2026, 1, 2, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Low", "Todo", "Archive old project files" },
                    { 2, "Chen", new DateTimeOffset(new DateTime(2026, 1, 3, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refresh the onboarding and on-call pages.", false, new DateTimeOffset(new DateTime(2026, 1, 4, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", "Todo", "Update team wiki" },
                    { 3, "Alice", new DateTimeOffset(new DateTime(2026, 1, 4, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Slides and a demo script for Friday's review.", false, new DateTimeOffset(new DateTime(2026, 1, 4, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High", "Todo", "Prepare sprint demo" },
                    { 4, "Dana", new DateTimeOffset(new DateTime(2026, 1, 5, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Users intermittently get 500s on sign-in.", false, new DateTimeOffset(new DateTime(2026, 1, 5, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Critical", "Todo", "Fix login outage" },
                    { 5, null, new DateTimeOffset(new DateTime(2026, 1, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, new DateTimeOffset(new DateTime(2026, 1, 7, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Low", "InProgress", "Tidy CSS variables" },
                    { 6, "Alice", new DateTimeOffset(new DateTime(2026, 1, 7, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cover filtering, validation and error responses.", false, new DateTimeOffset(new DateTime(2026, 1, 9, 16, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", "InProgress", "Write API integration tests" },
                    { 7, "Bob", new DateTimeOffset(new DateTime(2026, 1, 8, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Switch the build pipeline to the new runner pool.", false, new DateTimeOffset(new DateTime(2026, 1, 10, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High", "InProgress", "Migrate CI to new runners" },
                    { 8, "Chen", new DateTimeOffset(new DateTime(2026, 1, 9, 7, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Upgrade the affected dependency and redeploy.", false, new DateTimeOffset(new DateTime(2026, 1, 9, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Critical", "InProgress", "Patch security vulnerability" },
                    { 9, "Dana", new DateTimeOffset(new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Align issue labels with the new triage process.", false, new DateTimeOffset(new DateTime(2026, 1, 11, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Low", "Done", "Rename repository labels" },
                    { 10, null, new DateTimeOffset(new DateTime(2026, 1, 11, 13, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, false, new DateTimeOffset(new DateTime(2026, 1, 13, 17, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", "Done", "Draft onboarding guide" },
                    { 11, "Alice", new DateTimeOffset(new DateTime(2026, 1, 12, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Nightly full backups with 30-day retention.", false, new DateTimeOffset(new DateTime(2026, 1, 14, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High", "Done", "Set up database backups" },
                    { 12, "Eve", new DateTimeOffset(new DateTime(2026, 1, 13, 6, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Recover from the corrupted index incident.", false, new DateTimeOffset(new DateTime(2026, 1, 13, 18, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Critical", "Done", "Restore production database" },
                    { 13, "Chen", new DateTimeOffset(new DateTime(2026, 1, 14, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Created by mistake.", true, new DateTimeOffset(new DateTime(2026, 1, 14, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", "Todo", "Duplicate of \"Update team wiki\"" },
                    { 14, "Eve", new DateTimeOffset(new DateTime(2026, 1, 15, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cancelled after the budget review.", true, new DateTimeOffset(new DateTime(2026, 1, 16, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High", "InProgress", "Evaluate vendor X" },
                    { 15, null, new DateTimeOffset(new DateTime(2026, 1, 16, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, new DateTimeOffset(new DateTime(2026, 1, 16, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Low", "Done", "Old test task" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
