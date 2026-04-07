using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thekdar.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Name" = trim("Name")
                WHERE "Name" <> trim("Name");
                """);

            migrationBuilder.Sql("""
                UPDATE "Employees"
                SET "FirstName" = trim("FirstName"),
                    "LastName" = trim("LastName")
                WHERE "FirstName" <> trim("FirstName")
                   OR "LastName" <> trim("LastName");
                """);

            migrationBuilder.Sql("""
                WITH duplicate_names AS (
                    SELECT
                        "Id",
                        "LastName",
                        ROW_NUMBER() OVER (
                            PARTITION BY lower("FirstName"), lower("LastName")
                            ORDER BY "Id"
                        ) AS row_number
                    FROM "Employees"
                    WHERE "IsDeleted" = FALSE
                )
                UPDATE "Employees" AS employee
                SET "LastName" = left(duplicate."LastName" || ' ' || employee."Id", 50)
                FROM duplicate_names AS duplicate
                WHERE employee."Id" = duplicate."Id"
                  AND duplicate.row_number > 1;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Users"
                        GROUP BY "Name"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueNames: duplicate user names exist.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "Employees"
                        WHERE "IsDeleted" = FALSE
                        GROUP BY "FirstName", "LastName"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueNames: duplicate active employee names exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FirstName_LastName_Unique",
                table: "Employees",
                columns: new[] { "FirstName", "LastName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Name",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Employees_FirstName_LastName_Unique",
                table: "Employees");
        }
    }
}
