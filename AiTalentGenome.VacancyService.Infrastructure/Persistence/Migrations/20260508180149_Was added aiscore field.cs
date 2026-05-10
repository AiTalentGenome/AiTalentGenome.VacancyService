using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTalentGenome.VacancyService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Wasaddedaiscorefield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<string>>(
                name: "KeySkills",
                table: "Vacancies",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>(),
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValue: new List<string>());

            migrationBuilder.AddColumn<double>(
                name: "AiScore",
                table: "Applications",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidatePhone",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HhNegotiationId",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_HhNegotiationId",
                table: "Applications",
                column: "HhNegotiationId",
                unique: true,
                filter: "\"HhNegotiationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_HhNegotiationId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CandidatePhone",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "HhNegotiationId",
                table: "Applications");

            migrationBuilder.AlterColumn<List<string>>(
                name: "KeySkills",
                table: "Vacancies",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>(),
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValue: new List<string>());
        }
    }
}
