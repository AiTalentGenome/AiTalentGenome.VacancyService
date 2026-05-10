using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTalentGenome.VacancyService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewData : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "AiAnalysisJson",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "CandidateSkills",
                table: "Applications",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<List<string>>(
                name: "CriticalMismatches",
                table: "Applications",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<string>(
                name: "Education",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastCompany",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastJobTitle",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResumeText",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalExperienceMonths",
                table: "Applications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalysisJson",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CandidateSkills",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CriticalMismatches",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Education",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "LastCompany",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "LastJobTitle",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RawResumeText",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TotalExperienceMonths",
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
