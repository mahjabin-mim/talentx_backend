using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260502090000_AddAiScoringTables")]
    public partial class AddAiScoringTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.CreateTable(
                name: "AI_score",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_score", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AI_score_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AI_score_TalentProfiles_TalentId",
                        column: x => x.TalentId,
                        principalTable: "TalentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AI_score_refresh_queue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TalentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_score_refresh_queue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AI_score_refresh_queue_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AI_score_refresh_queue_TalentProfiles_TalentId",
                        column: x => x.TalentId,
                        principalTable: "TalentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_score_JobId_TalentId",
                table: "AI_score",
                columns: new[] { "JobId", "TalentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AI_score_TalentId",
                table: "AI_score",
                column: "TalentId");

            migrationBuilder.CreateIndex(
                name: "IX_AI_score_refresh_queue_JobId_TalentId",
                table: "AI_score_refresh_queue",
                columns: new[] { "JobId", "TalentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AI_score_refresh_queue_RequestedAt",
                table: "AI_score_refresh_queue",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AI_score_refresh_queue_TalentId",
                table: "AI_score_refresh_queue",
                column: "TalentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AI_score_refresh_queue");

            migrationBuilder.DropTable(
                name: "AI_score");
        }
    }
}
