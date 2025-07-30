using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAFERR.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxCodesPerMonth = table.Column<int>(type: "integer", nullable: false),
                    MaxVerificationsPerMonth = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodesGenerated = table.Column<int>(type: "integer", nullable: false),
                    VerificationsReceived = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandSubscriptions_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BrandSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    FirstVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityCodes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeAttempted = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    VerificationAttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    SourceIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SourcePhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SecurityCodeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationLogs_SecurityCodes_SecurityCodeId",
                        column: x => x.SecurityCodeId,
                        principalTable: "SecurityCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_CurrentSubscriptionId",
                table: "Brands",
                column: "CurrentSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandSubscriptions_BrandId_Status_StartDate_EndDate",
                table: "BrandSubscriptions",
                columns: new[] { "BrandId", "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandSubscriptions_Id",
                table: "BrandSubscriptions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BrandSubscriptions_SubscriptionPlanId",
                table: "BrandSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityCodes_Code",
                table: "SecurityCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityCodes_IsVerified",
                table: "SecurityCodes",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityCodes_ProductId",
                table: "SecurityCodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BrandId",
                table: "Users",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationLogs_CodeAttempted",
                table: "VerificationLogs",
                column: "CodeAttempted");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationLogs_SecurityCodeId",
                table: "VerificationLogs",
                column: "SecurityCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationLogs_SourcePhoneNumber",
                table: "VerificationLogs",
                column: "SourcePhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationLogs_VerificationAttemptedAt",
                table: "VerificationLogs",
                column: "VerificationAttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationLogs_VerificationAttemptedAt_Result",
                table: "VerificationLogs",
                columns: new[] { "VerificationAttemptedAt", "Result" });

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_BrandSubscriptions_CurrentSubscriptionId",
                table: "Brands",
                column: "CurrentSubscriptionId",
                principalTable: "BrandSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brands_BrandSubscriptions_CurrentSubscriptionId",
                table: "Brands");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VerificationLogs");

            migrationBuilder.DropTable(
                name: "SecurityCodes");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "BrandSubscriptions");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
