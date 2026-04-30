using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Thekdar.Migrations
{
    /// <inheritdoc />
    public partial class Initialcreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "User's full name"),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "User's email address - must be unique"),
                    Phone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "Phone number - exactly 10 digits"),
                    Age = table.Column<int>(type: "integer", nullable: false, comment: "User's age - must be between 18-100"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Account status: Active or Inactive"),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "User role: Admin or Contractor"),
                    PasswordHash = table.Column<string>(type: "text", nullable: false, comment: "BCrypt hashed password"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Account creation timestamp"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Last login timestamp"),
                    PasswordResetToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Password reset token"),
                    ResetTokenExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Password reset token expiration"),
                    ProfilePicture = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Profile picture as byte array"),
                    ProfilePictureContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "MIME type of profile picture (image/jpeg, image/png, etc.)"),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Employee's first name"),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Employee's last name"),
                    Trade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Employee's trade/specialization (Electrician, Plumber, etc.)"),
                    Phone1 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "Primary phone number - exactly 10 digits"),
                    Phone2 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, comment: "Secondary phone number - optional, exactly 10 digits if provided"),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Employee's email address"),
                    DailyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m, comment: "Hourly rate in dollars"),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Whether employee is available for work"),
                    PanNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "PAN number - optional"),
                    PanCardImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Path to PAN card image file"),
                    ProfilePicturePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Path to employee profile picture"),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Date employee was hired"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Soft delete flag - true means employee is deactivated"),
                    ContractorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_DistinctPhones", "\"Phone2\" IS NULL OR \"Phone1\" <> \"Phone2\"");
                    table.ForeignKey(
                        name: "FK_Employees_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "Job title/summary"),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Detailed job description"),
                    ClientName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Client/customer name"),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Job site address"),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scheduled date for the job (UTC)"),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active", comment: "Job status: Active, PendingConfirmation, or Completed"),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CompletedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "Job creation timestamp (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jobs_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payrolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ContractorId = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "date", nullable: false),
                    DaysWorked = table.Column<int>(type: "integer", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseWage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric", nullable: false),
                    OvertimeMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    OvertimeWage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Bonus = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BonusReason = table.Column<string>(type: "text", nullable: true),
                    Deduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeductionReason = table.Column<string>(type: "text", nullable: true),
                    TotalWage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetPayable = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "date", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    TransactionReference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PayslipGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    PayslipNumber = table.Column<string>(type: "text", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payrolls_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payrolls_Users_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "integer", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the employee was assigned"),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "When the assignment record was created"),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When work started (optional)"),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When work ended (optional)"),
                    HoursWorked = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m, comment: "Total hours worked on this job"),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Assistant", comment: "Role: Lead, Assistant, Apprentice"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Assigned", comment: "Status: Assigned, Working, Completed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobAssignments_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobAssignments_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobAssignments_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Contractor_Available_Deleted",
                table: "Employees",
                columns: new[] { "ContractorId", "IsAvailable", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ContractorId",
                table: "Employees",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email_Unique",
                table: "Employees",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FirstName_LastName_Unique",
                table: "Employees",
                columns: new[] { "FirstName", "LastName" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsAvailable",
                table: "Employees",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees",
                column: "IsDeleted");

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

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Trade",
                table: "Employees",
                column: "Trade");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_AssignedByUserId",
                table: "JobAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_EmployeeId",
                table: "JobAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_JobId",
                table: "JobAssignments",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_JobId_EmployeeId_Unique",
                table: "JobAssignments",
                columns: new[] { "JobId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_Status",
                table: "JobAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CompletedByUserId",
                table: "Jobs",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CreatedByUserId",
                table: "Jobs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ScheduledDate",
                table: "Jobs",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status",
                table: "Jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ContractorId",
                table: "Payrolls",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_EmployeeId",
                table: "Payrolls",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_PayslipNumber",
                table: "Payrolls",
                column: "PayslipNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_PeriodEnd",
                table: "Payrolls",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_PeriodStart",
                table: "Payrolls",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_Status",
                table: "Payrolls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAssignments");

            migrationBuilder.DropTable(
                name: "Payrolls");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
