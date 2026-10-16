using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSQL.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428120000_AddEmployerAndJobGuidDefaults")]
    public partial class AddEmployerAndJobGuidDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.Sql(
                "ALTER TABLE \"Employers\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
            migrationBuilder.Sql(
                "ALTER TABLE \"Jobs\" ALTER COLUMN \"Id\" SET DEFAULT gen_random_uuid();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Jobs\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Employers\" ALTER COLUMN \"Id\" DROP DEFAULT;");
        }
    }
}
