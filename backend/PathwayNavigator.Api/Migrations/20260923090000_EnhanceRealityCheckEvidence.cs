using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable

namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260923090000_EnhanceRealityCheckEvidence")]
public partial class EnhanceRealityCheckEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DegreeRequirement", "PathwayReviews", "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("SubjectRequirementsJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("EntryRequirementsJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("CostGuidance", "PathwayReviews", "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("GapClosurePlanJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("EvidenceSourcesJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "DegreeRequirement", "SubjectRequirementsJson", "EntryRequirementsJson", "CostGuidance", "GapClosurePlanJson", "EvidenceSourcesJson" })
            migrationBuilder.DropColumn(column, "PathwayReviews");
    }
}
