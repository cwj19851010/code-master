using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeMaster.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_mcp_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_prefix = table.Column<string>(type: "text", nullable: false),
                    scopes = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_ip = table.Column<string>(type: "text", nullable: true),
                    create_by = table.Column<string>(type: "text", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    update_user_id = table.Column<long>(type: "bigint", nullable: true),
                    update_by = table.Column<string>(type: "text", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remark = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_by = table.Column<string>(type: "text", nullable: true),
                    delete_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_mcp_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sys_mcp_tokens_token_hash",
                table: "sys_mcp_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_mcp_tokens_user_id",
                table: "sys_mcp_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_mcp_tokens");
        }
    }
}
