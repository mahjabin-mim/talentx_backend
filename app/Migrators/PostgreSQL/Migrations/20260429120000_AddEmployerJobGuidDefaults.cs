using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260429120000_AddEmployerJobGuidDefaults")]
    public partial class AddEmployerJobGuidDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Employers\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
            migrationBuilder.Sql(
                "ALTER TABLE \"Jobs\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Jobs\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Employers\" ALTER COLUMN \"Id\" DROP DEFAULT;");
        }
    }
}
