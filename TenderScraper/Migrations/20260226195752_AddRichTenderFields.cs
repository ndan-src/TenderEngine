using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderScraper.Migrations
{
    /// <inheritdoc />
    public partial class AddRichTenderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "Tenders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalCpvCodes",
                table: "Tenders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerCity",
                table: "Tenders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerContactEmail",
                table: "Tenders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerContactPhone",
                table: "Tenders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerCountry",
                table: "Tenders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerNameEn",
                table: "Tenders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerPortalUrl",
                table: "Tenders",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerWebsite",
                table: "Tenders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                table: "Tenders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNature",
                table: "Tenders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                table: "Tenders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpvCode",
                table: "Tenders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionDe",
                table: "Tenders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Tenders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotId",
                table: "Tenders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticeType",
                table: "Tenders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutsCode",
                table: "Tenders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcedureType",
                table: "Tenders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationDate",
                table: "Tenders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionDeadline",
                table: "Tenders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalCpvCodes",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerCity",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerContactEmail",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerContactPhone",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerCountry",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerNameEn",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerPortalUrl",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BuyerWebsite",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ContractNature",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "CpvCode",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "DescriptionDe",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "NoticeType",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "NutsCode",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ProcedureType",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "PublicationDate",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "SubmissionDeadline",
                table: "Tenders");

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "Tenders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
