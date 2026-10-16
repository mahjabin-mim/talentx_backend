using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260429121000_AddJobSkillsTable")]
    public partial class AddJobSkillsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.CreateTable(
                name: "JobSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSkills_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                WITH parsed AS (
                    SELECT
                        j."Id" AS "JobId",
                        trim(skill_value) AS "Name",
                        skill_position - 1 AS "OriginalDisplayOrder"
                    FROM "Jobs" j
                    CROSS JOIN LATERAL regexp_split_to_table(
                        replace(replace(coalesce(j."SkillRequirements", ''), chr(13), ','), chr(10), ','),
                        '[,;]+')
                        WITH ORDINALITY AS split(skill_value, skill_position)
                    WHERE trim(skill_value) <> ''
                ),
                deduplicated AS (
                    SELECT
                        "JobId",
                        "Name",
                        "OriginalDisplayOrder",
                        row_number() OVER (
                            PARTITION BY "JobId", lower("Name")
                            ORDER BY "OriginalDisplayOrder"
                        ) AS duplicate_rank
                    FROM parsed
                ),
                ordered AS (
                    SELECT
                        "JobId",
                        "Name",
                        row_number() OVER (
                            PARTITION BY "JobId"
                            ORDER BY "OriginalDisplayOrder"
                        ) - 1 AS "DisplayOrder"
                    FROM deduplicated
                    WHERE duplicate_rank = 1
                )
                INSERT INTO "JobSkills" ("JobId", "Name", "DisplayOrder")
                SELECT "JobId", left("Name", 120), "DisplayOrder"
                FROM ordered;
                """);

            migrationBuilder.DropColumn(
                name: "SkillRequirements",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_JobSkills_JobId",
                table: "JobSkills",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSkills_Name",
                table: "JobSkills",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillRequirements",
                table: "Jobs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Jobs" j
                SET "SkillRequirements" = left(coalesce(skill_summary."Skills", ''), 4000)
                FROM (
                    SELECT
                        "JobId",
                        string_agg("Name", ', ' ORDER BY "DisplayOrder") AS "Skills"
                    FROM "JobSkills"
                    GROUP BY "JobId"
                ) skill_summary
                WHERE j."Id" = skill_summary."JobId";
                """);

            migrationBuilder.DropTable(
                name: "JobSkills");
        }
    }
}
