using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CodeMaster.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_community_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_replies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    topic_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_reply_id = table.Column<long>(type: "bigint", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_community_replies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_topic_likes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    topic_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_community_topic_likes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_topics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    reply_count = table.Column<int>(type: "integer", nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    last_reply_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_community_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_table_columns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    table_id = table.Column<long>(type: "bigint", nullable: false),
                    column_name = table.Column<string>(type: "text", nullable: false),
                    property_name = table.Column<string>(type: "text", nullable: false),
                    column_comment = table.Column<string>(type: "text", nullable: true),
                    column_type = table.Column<string>(type: "text", nullable: true),
                    csharp_type = table.Column<string>(type: "text", nullable: true),
                    is_pk = table.Column<int>(type: "integer", nullable: false),
                    is_increment = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<int>(type: "integer", nullable: false),
                    show_in_list = table.Column<int>(type: "integer", nullable: false),
                    show_in_add = table.Column<int>(type: "integer", nullable: false),
                    show_in_edit = table.Column<int>(type: "integer", nullable: false),
                    show_in_detail = table.Column<int>(type: "integer", nullable: false),
                    is_query = table.Column<int>(type: "integer", nullable: false),
                    query_type = table.Column<string>(type: "text", nullable: true),
                    html_type = table.Column<string>(type: "text", nullable: true),
                    dict_type = table.Column<string>(type: "text", nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_table_columns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gen_tables",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: false),
                    entity_name = table.Column<string>(type: "text", nullable: false),
                    table_comment = table.Column<string>(type: "text", nullable: true),
                    function_name = table.Column<string>(type: "text", nullable: true),
                    module_id = table.Column<long>(type: "bigint", nullable: false),
                    is_read_only = table.Column<int>(type: "integer", nullable: false),
                    only_dto = table.Column<int>(type: "integer", nullable: false),
                    is_tree = table.Column<int>(type: "integer", nullable: false),
                    is_child = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    tree_parent_field = table.Column<string>(type: "text", nullable: true),
                    tree_name_field = table.Column<string>(type: "text", nullable: true),
                    function_author = table.Column<string>(type: "text", nullable: true),
                    gen_path = table.Column<string>(type: "text", nullable: true),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gen_tables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_modules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    module_name = table.Column<string>(type: "text", nullable: false),
                    module_description = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: true),
                    order_num = table.Column<int>(type: "integer", nullable: false),
                    route_path = table.Column<string>(type: "text", nullable: true),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    last_sync_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remark = table.Column<string>(type: "text", nullable: true),
                    create_by = table.Column<string>(type: "text", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    update_user_id = table.Column<long>(type: "bigint", nullable: true),
                    update_by = table.Column<string>(type: "text", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_by = table.Column<string>(type: "text", nullable: true),
                    delete_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_child_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    page_type = table.Column<string>(type: "text", nullable: false),
                    child_type = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    script_sections = table.Column<string>(type: "text", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_child_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_component_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    component_id = table.Column<long>(type: "bigint", nullable: false),
                    event_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: true),
                    type_description = table.Column<string>(type: "text", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    is_single = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_component_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_component_exposes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    component_id = table.Column<long>(type: "bigint", nullable: false),
                    expose_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    expose_type = table.Column<string>(type: "text", nullable: true),
                    type_description = table.Column<string>(type: "text", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_component_exposes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_component_groups",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    group_name = table.Column<string>(type: "text", nullable: false),
                    group_code = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_component_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_component_properties",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    component_id = table.Column<long>(type: "bigint", nullable: false),
                    prop_name = table.Column<string>(type: "text", nullable: false),
                    prop_type = table.Column<string>(type: "text", nullable: true),
                    type_description = table.Column<string>(type: "text", nullable: true),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    enum_values = table.Column<string>(type: "text", nullable: true),
                    is_advanced = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_component_properties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_component_slots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    component_id = table.Column<long>(type: "bigint", nullable: false),
                    slot_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    slot_type = table.Column<string>(type: "text", nullable: true),
                    type_description = table.Column<string>(type: "text", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_component_slots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_components",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false),
                    link = table.Column<string>(type: "text", nullable: false),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_components", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_depts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    ancestors = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_depts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_dict_datas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    dict_type = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    lang_key = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_datas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_dict_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    dict_name = table.Column<string>(type: "text", nullable: false),
                    dict_type = table.Column<string>(type: "text", nullable: false),
                    lang_key = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_directives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    directive_name = table.Column<string>(type: "text", nullable: false),
                    has_value = table.Column<bool>(type: "boolean", nullable: false),
                    value_type = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_directives", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_email_verifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    send_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_email_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_entity_field",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    module_entity_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_system_field = table.Column<bool>(type: "boolean", nullable: false),
                    data_type = table.Column<string>(type: "text", nullable: false),
                    is_nullable = table.Column<bool>(type: "boolean", nullable: false),
                    max_length = table.Column<int>(type: "integer", nullable: true),
                    precision = table.Column<int>(type: "integer", nullable: true),
                    scale = table.Column<int>(type: "integer", nullable: true),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    is_ignore = table.Column<bool>(type: "boolean", nullable: false),
                    is_primary_key = table.Column<bool>(type: "boolean", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    min_value = table.Column<string>(type: "text", nullable: true),
                    max_value = table.Column<string>(type: "text", nullable: true),
                    regex_pattern = table.Column<string>(type: "text", nullable: true),
                    is_email = table.Column<bool>(type: "boolean", nullable: false),
                    is_phone = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_list = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_detail = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_add_form = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_edit_form = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_search = table.Column<bool>(type: "boolean", nullable: false),
                    form_control_type = table.Column<string>(type: "text", nullable: false),
                    list_width = table.Column<int>(type: "integer", nullable: true),
                    order_num = table.Column<int>(type: "integer", nullable: false),
                    select_data_source = table.Column<string>(type: "text", nullable: true),
                    select_options = table.Column<string>(type: "text", nullable: true),
                    is_multiple = table.Column<bool>(type: "boolean", nullable: false),
                    related_entity_name = table.Column<string>(type: "text", nullable: true),
                    related_entity_id_field = table.Column<string>(type: "text", nullable: true),
                    related_entity_display_fields = table.Column<string>(type: "text", nullable: true),
                    field_category = table.Column<string>(type: "text", nullable: false),
                    formula = table.Column<string>(type: "text", nullable: true),
                    aggregate_type = table.Column<string>(type: "text", nullable: true),
                    aggregate_child_entity_id = table.Column<long>(type: "bigint", nullable: true),
                    aggregate_child_field_name = table.Column<string>(type: "text", nullable: true),
                    aggregate_separator = table.Column<string>(type: "text", nullable: true),
                    show_condition = table.Column<string>(type: "text", nullable: true),
                    is_sortable = table.Column<bool>(type: "boolean", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_entity_field", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_external_logins",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_user_id = table.Column<string>(type: "text", nullable: false),
                    provider_login = table.Column<string>(type: "text", nullable: true),
                    provider_email = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    last_login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_sys_external_logins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_field_control_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    control_type = table.Column<string>(type: "text", nullable: false),
                    page_section = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    script_sections = table.Column<string>(type: "text", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_field_control_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_file",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    real_name = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: false),
                    file_ext = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    store_path = table.Column<string>(type: "text", nullable: false),
                    access_url = table.Column<string>(type: "text", nullable: true),
                    store_type = table.Column<int>(type: "integer", nullable: false),
                    file_category = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_sys_file", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_lang_texts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    lang_code = table.Column<string>(type: "text", nullable: false),
                    lang_key = table.Column<string>(type: "text", nullable: false),
                    lang_value = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_sys_lang_texts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_langs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    lang_code = table.Column<string>(type: "text", nullable: false),
                    lang_name = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_langs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_login_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false),
                    login_ip = table.Column<string>(type: "text", nullable: false),
                    login_location = table.Column<string>(type: "text", nullable: true),
                    browser = table.Column<string>(type: "text", nullable: true),
                    os = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    msg = table.Column<string>(type: "text", nullable: true),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_sys_login_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_menus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    menu_name = table.Column<string>(type: "text", nullable: false),
                    title_key = table.Column<string>(type: "text", nullable: true),
                    order_num = table.Column<int>(type: "integer", nullable: false),
                    path = table.Column<string>(type: "text", nullable: true),
                    component = table.Column<string>(type: "text", nullable: true),
                    query = table.Column<string>(type: "text", nullable: true),
                    is_frame = table.Column<int>(type: "integer", nullable: false),
                    is_cache = table.Column<bool>(type: "boolean", nullable: false),
                    menu_type = table.Column<string>(type: "text", nullable: false),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    perms = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    menu_scope = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    ancestors = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_module_entity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    module_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    has_primary_key = table.Column<bool>(type: "boolean", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: true),
                    is_tree = table.Column<bool>(type: "boolean", nullable: false),
                    is_read_only = table.Column<bool>(type: "boolean", nullable: false),
                    has_tenant = table.Column<bool>(type: "boolean", nullable: false),
                    has_data_permission = table.Column<bool>(type: "boolean", nullable: false),
                    has_audit = table.Column<bool>(type: "boolean", nullable: false),
                    has_soft_delete = table.Column<bool>(type: "boolean", nullable: false),
                    generate_frontend = table.Column<bool>(type: "boolean", nullable: false),
                    frontend_route = table.Column<string>(type: "text", nullable: true),
                    menu_icon = table.Column<string>(type: "text", nullable: true),
                    order_num = table.Column<int>(type: "integer", nullable: false),
                    is_child_table = table.Column<bool>(type: "boolean", nullable: false),
                    is_generated = table.Column<bool>(type: "boolean", nullable: false),
                    last_generated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_module_entity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_one_to_many_relation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    module_entity_id = table.Column<long>(type: "bigint", nullable: false),
                    master_field = table.Column<string>(type: "text", nullable: false),
                    child_entity_id = table.Column<long>(type: "bigint", nullable: false),
                    child_entity_name = table.Column<string>(type: "text", nullable: false),
                    child_foreign_key = table.Column<string>(type: "text", nullable: false),
                    order_num = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_one_to_many_relation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_oper_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    business_type = table.Column<int>(type: "integer", nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    request_method = table.Column<string>(type: "text", nullable: false),
                    operator_type = table.Column<int>(type: "integer", nullable: false),
                    oper_name = table.Column<string>(type: "text", nullable: false),
                    oper_url = table.Column<string>(type: "text", nullable: false),
                    oper_ip = table.Column<string>(type: "text", nullable: false),
                    oper_location = table.Column<string>(type: "text", nullable: true),
                    oper_param = table.Column<string>(type: "text", nullable: true),
                    json_result = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_msg = table.Column<string>(type: "text", nullable: true),
                    oper_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    elapsed = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_sys_oper_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_page_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    page_type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false),
                    script_sections = table.Column<string>(type: "text", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_page_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_posts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    post_name = table.Column<string>(type: "text", nullable: false),
                    data_scope = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_project",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    project_name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    database_type = table.Column<int>(type: "integer", nullable: false),
                    connection_string = table.Column<string>(type: "text", nullable: false),
                    project_path = table.Column<string>(type: "text", nullable: false),
                    logo_path = table.Column<string>(type: "text", nullable: true),
                    project_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    frontend_port = table.Column<int>(type: "integer", nullable: true),
                    backend_port = table.Column<int>(type: "integer", nullable: true),
                    initialized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    init_error = table.Column<string>(type: "text", nullable: true),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_project", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_role_menus",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    menu_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_menus", x => new { x.role_id, x.menu_id });
                });

            migrationBuilder.CreateTable(
                name: "sys_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    role_name = table.Column<string>(type: "text", nullable: false),
                    role_key = table.Column<string>(type: "text", nullable: false),
                    role_sort = table.Column<int>(type: "integer", nullable: false),
                    data_scope = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    del_flag = table.Column<int>(type: "integer", nullable: false),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_task_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    task_id = table.Column<long>(type: "bigint", nullable: false),
                    task_name = table.Column<string>(type: "text", nullable: false),
                    invoke_target = table.Column<string>(type: "text", nullable: false),
                    job_message = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    elapsed = table.Column<double>(type: "double precision", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_task_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_tasks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    task_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    task_type = table.Column<int>(type: "integer", nullable: false),
                    invoke_target = table.Column<string>(type: "text", nullable: false),
                    cron_expression = table.Column<string>(type: "text", nullable: true),
                    interval_second = table.Column<int>(type: "integer", nullable: false),
                    run_times = table.Column<int>(type: "integer", nullable: false),
                    begin_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_sys_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_tenants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_code = table.Column<string>(type: "text", nullable: false),
                    tenant_name = table.Column<string>(type: "text", nullable: false),
                    isolation_type = table.Column<int>(type: "integer", nullable: false),
                    config_id = table.Column<string>(type: "text", nullable: true),
                    connection_string = table.Column<string>(type: "text", nullable: true),
                    db_type = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    expire_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_roles",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_roles", x => new { x.user_id, x.role_id });
                });

            migrationBuilder.CreateTable(
                name: "sys_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false),
                    nick_name = table.Column<string>(type: "text", nullable: false),
                    user_type = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    sex = table.Column<int>(type: "integer", nullable: true),
                    avatar = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    del_flag = table.Column<int>(type: "integer", nullable: false),
                    login_ip = table.Column<string>(type: "text", nullable: true),
                    login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    post_id = table.Column<long>(type: "bigint", nullable: true),
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
                    delete_user_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    dept_id = table.Column<long>(type: "bigint", nullable: true),
                    dept_ancestors = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_users", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_categories");

            migrationBuilder.DropTable(
                name: "community_replies");

            migrationBuilder.DropTable(
                name: "community_topic_likes");

            migrationBuilder.DropTable(
                name: "community_topics");

            migrationBuilder.DropTable(
                name: "gen_table_columns");

            migrationBuilder.DropTable(
                name: "gen_tables");

            migrationBuilder.DropTable(
                name: "project_modules");

            migrationBuilder.DropTable(
                name: "sys_child_templates");

            migrationBuilder.DropTable(
                name: "sys_component_events");

            migrationBuilder.DropTable(
                name: "sys_component_exposes");

            migrationBuilder.DropTable(
                name: "sys_component_groups");

            migrationBuilder.DropTable(
                name: "sys_component_properties");

            migrationBuilder.DropTable(
                name: "sys_component_slots");

            migrationBuilder.DropTable(
                name: "sys_components");

            migrationBuilder.DropTable(
                name: "sys_depts");

            migrationBuilder.DropTable(
                name: "sys_dict_datas");

            migrationBuilder.DropTable(
                name: "sys_dict_types");

            migrationBuilder.DropTable(
                name: "sys_directives");

            migrationBuilder.DropTable(
                name: "sys_email_verifications");

            migrationBuilder.DropTable(
                name: "sys_entity_field");

            migrationBuilder.DropTable(
                name: "sys_external_logins");

            migrationBuilder.DropTable(
                name: "sys_field_control_templates");

            migrationBuilder.DropTable(
                name: "sys_file");

            migrationBuilder.DropTable(
                name: "sys_lang_texts");

            migrationBuilder.DropTable(
                name: "sys_langs");

            migrationBuilder.DropTable(
                name: "sys_login_log");

            migrationBuilder.DropTable(
                name: "sys_menus");

            migrationBuilder.DropTable(
                name: "sys_module_entity");

            migrationBuilder.DropTable(
                name: "sys_one_to_many_relation");

            migrationBuilder.DropTable(
                name: "sys_oper_log");

            migrationBuilder.DropTable(
                name: "sys_page_templates");

            migrationBuilder.DropTable(
                name: "sys_posts");

            migrationBuilder.DropTable(
                name: "sys_project");

            migrationBuilder.DropTable(
                name: "sys_role_menus");

            migrationBuilder.DropTable(
                name: "sys_roles");

            migrationBuilder.DropTable(
                name: "sys_task_logs");

            migrationBuilder.DropTable(
                name: "sys_tasks");

            migrationBuilder.DropTable(
                name: "sys_tenants");

            migrationBuilder.DropTable(
                name: "sys_user_roles");

            migrationBuilder.DropTable(
                name: "sys_users");
        }
    }
}
