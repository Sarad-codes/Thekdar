using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thekdar.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Email" = lower(trim("Email"))
                WHERE "Email" <> lower(trim("Email"));
                """);

            migrationBuilder.Sql("""
                UPDATE "Employees"
                SET "Email" = lower(trim("Email"))
                WHERE "Email" IS NOT NULL
                  AND "Email" <> lower(trim("Email"));
                """);

            migrationBuilder.Sql("""
                WITH duplicate_emails AS (
                    SELECT
                        "Id",
                        "Email",
                        ROW_NUMBER() OVER (PARTITION BY lower("Email") ORDER BY "Id") AS row_number
                    FROM "Employees"
                    WHERE "Email" IS NOT NULL
                      AND "IsDeleted" = FALSE
                )
                UPDATE "Employees" AS employee
                SET "Email" =
                    CASE
                        WHEN position('@' IN duplicate."Email") > 0 THEN
                            split_part(duplicate."Email", '@', 1) || '+employee' || employee."Id" || '@' || split_part(duplicate."Email", '@', 2)
                        ELSE
                            duplicate."Email" || '+employee' || employee."Id"
                    END
                FROM duplicate_emails AS duplicate
                WHERE employee."Id" = duplicate."Id"
                  AND duplicate.row_number > 1;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Users"
                        GROUP BY "Phone"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueContacts: duplicate user phone numbers exist.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "Employees"
                        WHERE "Phone2" IS NOT NULL
                          AND "Phone1" = "Phone2"
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueContacts: some employees use the same primary and secondary phone number.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT "Phone1" AS phone
                            FROM "Employees"
                            WHERE "IsDeleted" = FALSE

                            UNION ALL

                            SELECT "Phone2" AS phone
                            FROM "Employees"
                            WHERE "IsDeleted" = FALSE
                              AND "Phone2" IS NOT NULL
                        ) AS employee_phones
                        GROUP BY phone
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueContacts: duplicate active employee phone numbers exist.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "Employees"
                        WHERE "IsDeleted" = FALSE
                          AND "PanNumber" IS NOT NULL
                        GROUP BY lower("PanNumber")
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot apply EnforceUniqueContacts: duplicate active employee PAN numbers exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email_Unique",
                table: "Employees",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PanNumber_Unique",
                table: "Employees",
                column: "PanNumber",
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Phone1_Unique",
                table: "Employees",
                column: "Phone1",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Phone2_Unique",
                table: "Employees",
                column: "Phone2",
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"Phone2\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_DistinctPhones",
                table: "Employees",
                sql: "\"Phone2\" IS NULL OR \"Phone1\" <> \"Phone2\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email_Unique",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PanNumber_Unique",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Phone1_Unique",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Phone2_Unique",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_DistinctPhones",
                table: "Employees");
        }
    }
}
