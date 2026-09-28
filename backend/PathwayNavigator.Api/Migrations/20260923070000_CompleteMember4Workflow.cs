using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable

namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260923070000_CompleteMember4Workflow")]
public partial class CompleteMember4Workflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AgentError", "PathwayReviews", "text", nullable: true);
        migrationBuilder.AddColumn<string>("AgentStatus", "PathwayReviews", "character varying(40)", maxLength: 40, nullable: false, defaultValue: "approval_required");
        migrationBuilder.AddColumn<string>("ExecutionTraceJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<int>("FeasibilityScore", "PathwayReviews", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>("TargetCareer", "PathwayReviews", "character varying(120)", maxLength: 120, nullable: false, defaultValue: "Unknown");
        migrationBuilder.AddColumn<string>("ToolCallsJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<DateTime>("UpdatedAt", "PathwayReviews", "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        migrationBuilder.AddColumn<string>("ValidationResultsJson", "PathwayReviews", "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("WorkflowId", "PathwayReviews", "character varying(100)", maxLength: 100, nullable: false, defaultValueSql: "gen_random_uuid()::text");

        migrationBuilder.CreateTable(
            name: "PathwayReviewAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PathwayReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PathwayReviewAudits", x => x.Id);
                table.ForeignKey("FK_PathwayReviewAudits_PathwayReviews_PathwayReviewId", x => x.PathwayReviewId,
                    "PathwayReviews", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_PathwayReviews_Status_CreatedAt", "PathwayReviews", new[] { "Status", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_PathwayReviews_WorkflowId", "PathwayReviews", "WorkflowId", unique: true);
        migrationBuilder.CreateIndex("IX_PathwayReviewAudits_PathwayReviewId", "PathwayReviewAudits", "PathwayReviewId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PathwayReviewAudits");
        migrationBuilder.DropIndex("IX_PathwayReviews_Status_CreatedAt", "PathwayReviews");
        migrationBuilder.DropIndex("IX_PathwayReviews_WorkflowId", "PathwayReviews");
        foreach (var column in new[] { "AgentError", "AgentStatus", "ExecutionTraceJson", "FeasibilityScore", "TargetCareer", "ToolCallsJson", "UpdatedAt", "ValidationResultsJson", "WorkflowId" })
            migrationBuilder.DropColumn(column, "PathwayReviews");
    }
}
