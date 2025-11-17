using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareWork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesAndTagsToCheckin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Checkins",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Checkins",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Checkins_UserId_CreatedAt",
                table: "Checkins",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Checkins_UserId_CreatedAt",
                table: "Checkins");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Checkins");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Checkins");
        }
    }
}
