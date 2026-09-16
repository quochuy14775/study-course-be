using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyCourseAPI.Migrations
{
    /// <inheritdoc />
    public partial class Brand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandColor",
                table: "Languages",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandColor",
                table: "Frameworks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Frameworks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrandColor",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "BrandColor",
                table: "Frameworks");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Frameworks");
        }
    }
}
