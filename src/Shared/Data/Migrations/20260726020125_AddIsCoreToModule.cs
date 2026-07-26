using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiskyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCoreToModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_core",
                table: "modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_core",
                table: "modules");
        }
    }
}
