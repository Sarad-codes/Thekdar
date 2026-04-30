using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thekdar.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileAccessToEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMobileLogin",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MobileEnabled",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MobilePasswordHash",
                table: "Employees",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMobileLogin",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MobileEnabled",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MobilePasswordHash",
                table: "Employees");
        }
    }
}
