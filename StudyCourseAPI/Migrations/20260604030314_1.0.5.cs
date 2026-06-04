using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyCourseAPI.Migrations
{
    /// <inheritdoc />
    public partial class _105 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Articles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Articles");
        }
    }
}
