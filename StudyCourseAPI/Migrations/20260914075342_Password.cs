using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyCourseAPI.Migrations
{
    /// <inheritdoc />
    public partial class Password : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PasswordResetOtpAttempts",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetOtpExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetOtpHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetOtpAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetOtpExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetOtpHash",
                table: "AspNetUsers");
        }
    }
}
