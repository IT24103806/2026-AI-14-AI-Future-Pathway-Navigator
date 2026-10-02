using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable

namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261002080000_AddPathwayPlansTable")]
public partial class AddPathwayPlansTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PathwayPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkflowId = table.Column<string>(type: "text", nullable: false),
                SelectedPathway = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                RoadmapJson = table.Column<string>(type: "text", nullable: false),
                MissingSkillsJson = table.Column<string>(type: "text", nullable: false),
                CompletedPhasesJson = table.Column<string>(type: "text", nullable: false),
                NextAction = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PathwayPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_PathwayPlans_StudentProfiles_StudentProfileId",
                    column: x => x.StudentProfileId,
                    principalTable: "StudentProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PathwayPlans_StudentProfileId_SelectedPathway",
            table: "PathwayPlans",
            columns: new[] { "StudentProfileId", "SelectedPathway" });

        migrationBuilder.CreateIndex(
            name: "IX_PathwayPlans_StudentProfileId_UpdatedAt",
            table: "PathwayPlans",
            columns: new[] { "StudentProfileId", "UpdatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PathwayPlans");
    }
}
