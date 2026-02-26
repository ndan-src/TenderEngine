using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderScraper.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    TenderID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TitleDe = table.Column<string>(type: "text", nullable: true),
                    TitleEn = table.Column<string>(type: "text", nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ValueEuro = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuitabilityScore = table.Column<decimal>(type: "numeric(3,1)", nullable: true),
                    RawXml = table.Column<string>(type: "text", nullable: true),
                    EnglishExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    FatalFlaws = table.Column<string>(type: "text", nullable: true),
                    HardCertifications = table.Column<string>(type: "text", nullable: true),
                    TechStack = table.Column<string>(type: "text", nullable: true),
                    EligibilityProbability = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.TenderID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_SourceId",
                table: "Tenders",
                column: "SourceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenders");
        }
    }
}
