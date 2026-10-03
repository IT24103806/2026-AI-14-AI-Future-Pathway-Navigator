using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PathwayNavigator.Api.Data;

#nullable disable

namespace PathwayNavigator.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261003090000_AddConsultationSupport")]
public partial class AddConsultationSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ---------------------------------------------------------------- Consultant role (idempotent)
        // Existing databases already hold Student/Counsellor/Admin from 20260924153916_SeedDefaultRoles,
        // so the fourth role is inserted here rather than relying on the startup seeder.
        migrationBuilder.Sql(
            "INSERT INTO \"Roles\" (\"Id\", \"Name\", \"CreatedAt\", \"UpdatedAt\") " +
            "VALUES ('10000000-0000-0000-0000-000000000004', 'Consultant', TIMESTAMPTZ '2026-01-01 00:00:00Z', TIMESTAMPTZ '2026-01-01 00:00:00Z') " +
            "ON CONFLICT (\"Id\") DO NOTHING;");

        migrationBuilder.CreateTable(
            name: "ConsultantProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Headline = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                ExpertiseJson = table.Column<string>(type: "text", nullable: false),
                LanguagesJson = table.Column<string>(type: "text", nullable: false),
                IsAcceptingRequests = table.Column<bool>(type: "boolean", nullable: false),
                MaxOpenCases = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsultantProfiles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ConsultationRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedConsultantId = table.Column<Guid>(type: "uuid", nullable: true),
                ContextType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ContextRefId = table.Column<Guid>(type: "uuid", nullable: true),
                ContextSnapshotJson = table.Column<string>(type: "text", nullable: false),
                Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Priority = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                Subject = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                SlaDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FirstRespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolutionSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ConsultantGuidanceJson = table.Column<string>(type: "text", nullable: false),
                AgentTriageJson = table.Column<string>(type: "text", nullable: true),
                AgentDraftUsed = table.Column<bool>(type: "boolean", nullable: false),
                StudentRating = table.Column<int>(type: "integer", nullable: true),
                StudentFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                RowVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsultationRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsultationRequests_Users_AssignedConsultantId",
                    column: x => x.AssignedConsultantId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ConsultationRequests_Users_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ConsultationMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConsultationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                ResourcesJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsultationMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsultationMessages_ConsultationRequests_ConsultationRequestId",
                    column: x => x.ConsultationRequestId,
                    principalTable: "ConsultationRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ConsultationMessages_Users_AuthorUserId",
                    column: x => x.AuthorUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ConsultationAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConsultationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsultationAudits", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsultationAudits_ConsultationRequests_ConsultationRequestId",
                    column: x => x.ConsultationRequestId,
                    principalTable: "ConsultationRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ConsultationAudits_Users_ActorUserId",
                    column: x => x.ActorUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Body = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                DeepLink = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DedupeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ------------------------------------------------- Write-back fields on the journey entities
        migrationBuilder.AddColumn<string>(
            name: "ConsultantAdviceJson",
            table: "PathwayReviews",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "ConsultantGuidanceJson",
            table: "PathwayPlans",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        // ------------------------------------------------------------------------------- Indexes
        migrationBuilder.CreateIndex(
            name: "IX_ConsultantProfiles_UserId",
            table: "ConsultantProfiles",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationRequests_AssignedConsultantId_Status",
            table: "ConsultationRequests",
            columns: new[] { "AssignedConsultantId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationRequests_ContextType_ContextRefId",
            table: "ConsultationRequests",
            columns: new[] { "ContextType", "ContextRefId" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationRequests_Status_Priority_CreatedAt",
            table: "ConsultationRequests",
            columns: new[] { "Status", "Priority", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationRequests_StudentId_CreatedAt",
            table: "ConsultationRequests",
            columns: new[] { "StudentId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationAudits_ConsultationRequestId_CreatedAt",
            table: "ConsultationAudits",
            columns: new[] { "ConsultationRequestId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationAudits_ActorUserId",
            table: "ConsultationAudits",
            column: "ActorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationMessages_ConsultationRequestId_CreatedAt",
            table: "ConsultationMessages",
            columns: new[] { "ConsultationRequestId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultationMessages_AuthorUserId",
            table: "ConsultationMessages",
            column: "AuthorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_IsRead_CreatedAt",
            table: "Notifications",
            columns: new[] { "UserId", "IsRead", "CreatedAt" });

        // Partial unique index: many notifications may have a NULL DedupeKey, but an event key
        // (e.g. "sla-breach:{requestId}") can only produce one notification.
        migrationBuilder.CreateIndex(
            name: "IX_Notifications_DedupeKey",
            table: "Notifications",
            column: "DedupeKey",
            unique: true,
            filter: "\"DedupeKey\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Notifications_DedupeKey", table: "Notifications");
        migrationBuilder.DropIndex(name: "IX_Notifications_UserId_IsRead_CreatedAt", table: "Notifications");
        migrationBuilder.DropIndex(name: "IX_ConsultationMessages_AuthorUserId", table: "ConsultationMessages");
        migrationBuilder.DropIndex(name: "IX_ConsultationMessages_ConsultationRequestId_CreatedAt", table: "ConsultationMessages");
        migrationBuilder.DropIndex(name: "IX_ConsultationAudits_ActorUserId", table: "ConsultationAudits");
        migrationBuilder.DropIndex(name: "IX_ConsultationAudits_ConsultationRequestId_CreatedAt", table: "ConsultationAudits");
        migrationBuilder.DropIndex(name: "IX_ConsultationRequests_StudentId_CreatedAt", table: "ConsultationRequests");
        migrationBuilder.DropIndex(name: "IX_ConsultationRequests_Status_Priority_CreatedAt", table: "ConsultationRequests");
        migrationBuilder.DropIndex(name: "IX_ConsultationRequests_ContextType_ContextRefId", table: "ConsultationRequests");
        migrationBuilder.DropIndex(name: "IX_ConsultationRequests_AssignedConsultantId_Status", table: "ConsultationRequests");
        migrationBuilder.DropIndex(name: "IX_ConsultantProfiles_UserId", table: "ConsultantProfiles");

        migrationBuilder.DropColumn(name: "ConsultantGuidanceJson", table: "PathwayPlans");
        migrationBuilder.DropColumn(name: "ConsultantAdviceJson", table: "PathwayReviews");

        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "ConsultationAudits");
        migrationBuilder.DropTable(name: "ConsultationMessages");
        migrationBuilder.DropTable(name: "ConsultationRequests");
        migrationBuilder.DropTable(name: "ConsultantProfiles");

        migrationBuilder.Sql("DELETE FROM \"Roles\" WHERE \"Id\" = '10000000-0000-0000-0000-000000000004';");
    }
}
