using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423101500_EnsureApplicationsTableAndResumeFields")]
    public partial class EnsureApplicationsTableAndResumeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Applications" (
                    "Id" uuid NOT NULL,
                    "TalentId" uuid NULL,
                    "JobId" integer NULL,
                    "Name" character varying(100) NULL,
                    "ExpectedSalary" numeric(18,2) NOT NULL,
                    "Email" character varying(255) NULL,
                    "Experience" integer NOT NULL,
                    "Location" character varying(100) NULL,
                    "Education" text NULL,
                    "Department" character varying(255) NULL,
                    "CGPA" character varying(50) NULL,
                    "ResumeId" character varying(255) NULL,
                    "LinkedInUrl" character varying(255) NULL,
                    "AdditionalUrl" character varying(255) NULL,
                    "CoverLetter" text NULL,
                    "ContactNumber" character varying(20) NULL,
                    "Status" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_Applications" PRIMARY KEY ("Id")
                );

                ALTER TABLE IF EXISTS "Applications"
                    ADD COLUMN IF NOT EXISTS "Education" text NULL;

                ALTER TABLE IF EXISTS "Applications"
                    ADD COLUMN IF NOT EXISTS "Department" character varying(255) NULL;

                ALTER TABLE IF EXISTS "Applications"
                    ADD COLUMN IF NOT EXISTS "CGPA" character varying(50) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS "Applications"
                    DROP COLUMN IF EXISTS "CGPA";

                ALTER TABLE IF EXISTS "Applications"
                    DROP COLUMN IF EXISTS "Department";

                ALTER TABLE IF EXISTS "Applications"
                    DROP COLUMN IF EXISTS "Education";
                """);
        }
    }
}
