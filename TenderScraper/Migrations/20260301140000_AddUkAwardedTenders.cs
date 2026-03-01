using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddUkAwardedTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UkAwardedTenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    Ocid = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReleaseId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),

                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CpvCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CpvDescription = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AdditionalCpvCodes = table.Column<string>(type: "text", nullable: true),
                    ProcurementMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProcurementMethodDetails = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MainProcurementCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TenderValueAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TenderValueCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SuitableSme = table.Column<bool>(type: "boolean", nullable: true),
                    SuitableVcse = table.Column<bool>(type: "boolean", nullable: true),
                    TenderDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenderContractStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenderContractEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),

                    DeliveryRegion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeliveryCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),

                    BuyerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuyerStreetAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BuyerLocality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BuyerCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BuyerContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BuyerContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),

                    AwardId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AwardStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AwardDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AwardDatePublished = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AwardValueAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AwardValueCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AwardContractStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AwardContractEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),

                    SupplierNames = table.Column<string>(type: "text", nullable: true),
                    SupplierIds = table.Column<string>(type: "text", nullable: true),
                    SupplierScale = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),

                    NoticeUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UkAwardedTenders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UkAwardedTenders_Ocid",
                table: "UkAwardedTenders",
                column: "Ocid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UkAwardedTenders");
        }
    }
}

