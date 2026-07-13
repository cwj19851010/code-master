using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeMaster.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAdminFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_tenant_admin",
                table: "sys_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_tenant_admin",
                table: "sys_roles");
        }
    }
}
