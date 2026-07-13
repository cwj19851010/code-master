using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeMaster.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gen_table_columns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    table_id = table.Column<long>(type: "bigint", nullable: false),
                    column_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    property_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    column_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    column_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    csharp_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_pk = table.Column<int>(type: "int", nullable: false),
                    is_increment = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<int>(type: "int", nullable: false),
                    show_in_list = table.Column<int>(type: "int", nullable: false),
                    show_in_add = table.Column<int>(type: "int", nullable: false),
                    show_in_edit = table.Column<int>(type: "int", nullable: false),
                    show_in_detail = table.Column<int>(type: "int", nullable: false),
                    is_query = table.Column<int>(type: "int", nullable: false),
                    query_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    html_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dict_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sort = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    entity_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    table_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    function_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    module_id = table.Column<long>(type: "bigint", nullable: false),
                    is_read_only = table.Column<int>(type: "int", nullable: false),
                    only_dto = table.Column<int>(type: "int", nullable: false),
                    is_tree = table.Column<int>(type: "int", nullable: false),
                    is_child = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    tree_parent_field = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tree_name_field = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    function_author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gen_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    module_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    module_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order_num = table.Column<int>(type: "int", nullable: false),
                    route_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_synced = table.Column<bool>(type: "bit", nullable: false),
                    last_sync_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_depts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    ancestors = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    dict_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lang_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_default = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    sort = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    dict_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    dict_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lang_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    sort = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_dict_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_entity_field",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    module_entity_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_system_field = table.Column<bool>(type: "bit", nullable: false),
                    data_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_nullable = table.Column<bool>(type: "bit", nullable: false),
                    max_length = table.Column<int>(type: "int", nullable: true),
                    precision = table.Column<int>(type: "int", nullable: true),
                    scale = table.Column<int>(type: "int", nullable: true),
                    default_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_ignore = table.Column<bool>(type: "bit", nullable: false),
                    is_primary_key = table.Column<bool>(type: "bit", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    min_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    max_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    regex_pattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_email = table.Column<bool>(type: "bit", nullable: false),
                    is_phone = table.Column<bool>(type: "bit", nullable: false),
                    show_in_list = table.Column<bool>(type: "bit", nullable: false),
                    show_in_detail = table.Column<bool>(type: "bit", nullable: false),
                    show_in_add_form = table.Column<bool>(type: "bit", nullable: false),
                    show_in_edit_form = table.Column<bool>(type: "bit", nullable: false),
                    show_in_search = table.Column<bool>(type: "bit", nullable: false),
                    form_control_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    list_width = table.Column<int>(type: "int", nullable: true),
                    order_num = table.Column<int>(type: "int", nullable: false),
                    select_data_source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    select_options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_entity_field", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_file",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    real_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_ext = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    store_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    access_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    store_type = table.Column<int>(type: "int", nullable: false),
                    file_category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    lang_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lang_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lang_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    lang_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lang_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_default = table.Column<int>(type: "int", nullable: false),
                    is_enabled = table.Column<int>(type: "int", nullable: false),
                    sort = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    user_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    login_ip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    login_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    browser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    msg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    login_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    menu_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    title_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order_num = table.Column<int>(type: "int", nullable: false),
                    path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    component = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_frame = table.Column<int>(type: "int", nullable: false),
                    is_cache = table.Column<int>(type: "int", nullable: false),
                    menu_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    visible = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    perms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    menu_scope = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    ancestors = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    table_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_tree = table.Column<bool>(type: "bit", nullable: false),
                    is_read_only = table.Column<bool>(type: "bit", nullable: false),
                    has_tenant = table.Column<bool>(type: "bit", nullable: false),
                    has_data_permission = table.Column<bool>(type: "bit", nullable: false),
                    generate_frontend = table.Column<bool>(type: "bit", nullable: false),
                    frontend_route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    menu_icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    order_num = table.Column<int>(type: "int", nullable: false),
                    is_generated = table.Column<bool>(type: "bit", nullable: false),
                    last_generated_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_module_entity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_oper_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    business_type = table.Column<int>(type: "int", nullable: false),
                    method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    request_method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    operator_type = table.Column<int>(type: "int", nullable: false),
                    oper_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    oper_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    oper_ip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    oper_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    oper_param = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    json_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    error_msg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    oper_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    elapsed = table.Column<long>(type: "bigint", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_oper_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_posts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    post_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_scope = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    project_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_name_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    database_type = table.Column<int>(type: "int", nullable: false),
                    connection_string = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    project_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    logo_path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    project_type = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    frontend_port = table.Column<int>(type: "int", nullable: true),
                    backend_port = table.Column<int>(type: "int", nullable: true),
                    initialized_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    init_error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    role_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role_sort = table.Column<int>(type: "int", nullable: false),
                    data_scope = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    del_flag = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    task_id = table.Column<long>(type: "bigint", nullable: false),
                    task_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    invoke_target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    job_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    elapsed = table.Column<double>(type: "float", nullable: false),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    task_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    job_group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    task_type = table.Column<int>(type: "int", nullable: false),
                    invoke_target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cron_expression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    interval_second = table.Column<int>(type: "int", nullable: false),
                    run_times = table.Column<int>(type: "int", nullable: false),
                    begin_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_run_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    tenant_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tenant_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isolation_type = table.Column<int>(type: "int", nullable: false),
                    config_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    connection_string = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    db_type = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    expire_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    user_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nick_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    user_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sex = table.Column<int>(type: "int", nullable: true),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    del_flag = table.Column<int>(type: "int", nullable: false),
                    login_ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    login_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    post_id = table.Column<long>(type: "bigint", nullable: true),
                    create_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    create_user_id = table.Column<long>(type: "bigint", nullable: true),
                    create_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    update_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    delete_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    delete_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    dept_id = table.Column<long>(type: "bigint", nullable: true),
                    dept_ancestors = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "gen_table_columns");

            migrationBuilder.DropTable(
                name: "gen_tables");

            migrationBuilder.DropTable(
                name: "project_modules");

            migrationBuilder.DropTable(
                name: "sys_depts");

            migrationBuilder.DropTable(
                name: "sys_dict_datas");

            migrationBuilder.DropTable(
                name: "sys_dict_types");

            migrationBuilder.DropTable(
                name: "sys_entity_field");

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
                name: "sys_oper_log");

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
