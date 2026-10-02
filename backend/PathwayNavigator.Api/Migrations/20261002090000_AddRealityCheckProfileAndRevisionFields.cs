using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable

namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002090000_AddRealityCheckProfileAndRevisionFields")]
public partial class AddRealityCheckProfileAndRevisionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AlStream",
            table: "StudentProfiles",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "AlResults",
            table: "StudentProfiles",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "BudgetLevel",
            table: "StudentProfiles",
            type: "text",
            nullable: false,
            defaultValue: "Medium");

        migrationBuilder.AddColumn<string>(
            name: "AlStream",
            table: "PathwayReviews",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "AlResults",
            table: "PathwayReviews",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "BudgetLevel",
            table: "PathwayReviews",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Medium");

        migrationBuilder.AddColumn<string>(
            name: "CurrentSkillsJson",
            table: "PathwayReviews",
            type: "text",
            nullable: false,
            defaultValue: "[]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AlStream", table: "StudentProfiles");
        migrationBuilder.DropColumn(name: "AlResults", table: "StudentProfiles");
        migrationBuilder.DropColumn(name: "BudgetLevel", table: "StudentProfiles");
        migrationBuilder.DropColumn(name: "AlStream", table: "PathwayReviews");
        migrationBuilder.DropColumn(name: "AlResults", table: "PathwayReviews");
        migrationBuilder.DropColumn(name: "BudgetLevel", table: "PathwayReviews");
        migrationBuilder.DropColumn(name: "CurrentSkillsJson", table: "PathwayReviews");
    }
}
