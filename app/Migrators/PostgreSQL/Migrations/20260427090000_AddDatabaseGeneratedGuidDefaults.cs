using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260427090000_AddDatabaseGeneratedGuidDefaults")]
    public partial class AddDatabaseGeneratedGuidDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
            migrationBuilder.Sql(
                "ALTER TABLE \"TalentProfiles\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
            migrationBuilder.Sql(
                "ALTER TABLE \"Applications\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Applications\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE \"TalentProfiles\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Users\" ALTER COLUMN \"Id\" DROP DEFAULT;");
        }
    }
}
