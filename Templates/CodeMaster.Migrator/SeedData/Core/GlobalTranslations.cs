namespace CodeMaster.Migrator.SeedData.Core;

/// <summary>
/// 全局通用翻译键
/// </summary>
public static class GlobalTranslations
{
    public static Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            // 应用标题
            { "app_title", new() { { "zh-CN", "CodeMaster" }, { "en-US", "CodeMaster" } } },
            { "login_title", new() { { "zh-CN", "CodeMaster 管理系统" }, { "en-US", "CodeMaster Admin System" } } },

            // 用户操作
            { "profile", new() { { "zh-CN", "个人中心" }, { "en-US", "Profile" } } },
            { "logout", new() { { "zh-CN", "退出登录" }, { "en-US", "Logout" } } },
            { "logout_confirm", new() { { "zh-CN", "确定要退出登录吗？" }, { "en-US", "Are you sure you want to logout?" } } },
            { "logout_success", new() { { "zh-CN", "退出成功" }, { "en-US", "Logout Success" } } },
            { "prompt", new() { { "zh-CN", "提示" }, { "en-US", "Prompt" } } },
            { "admin", new() { { "zh-CN", "管理员" }, { "en-US", "Admin" } } },

            // 基础操作
            { "add", new() { { "zh-CN", "新增" }, { "en-US", "Add" } } },
            { "edit", new() { { "zh-CN", "编辑" }, { "en-US", "Edit" } } },
            { "delete", new() { { "zh-CN", "删除" }, { "en-US", "Delete" } } },
            { "batch_delete", new() { { "zh-CN", "批量删除" }, { "en-US", "Batch Delete" } } },
            { "detail", new() { { "zh-CN", "详情" }, { "en-US", "Detail" } } },
            { "view", new() { { "zh-CN", "查看" }, { "en-US", "View" } } },
            { "search", new() { { "zh-CN", "搜索" }, { "en-US", "Search" } } },
            { "query", new() { { "zh-CN", "查询" }, { "en-US", "Query" } } },
            { "reset", new() { { "zh-CN", "重置" }, { "en-US", "Reset" } } },
            { "submit", new() { { "zh-CN", "提交" }, { "en-US", "Submit" } } },
            { "cancel", new() { { "zh-CN", "取消" }, { "en-US", "Cancel" } } },
            { "confirm", new() { { "zh-CN", "确认" }, { "en-US", "Confirm" } } },
            { "save", new() { { "zh-CN", "保存" }, { "en-US", "Save" } } },
            { "export", new() { { "zh-CN", "导出" }, { "en-US", "Export" } } },
            { "import", new() { { "zh-CN", "导入" }, { "en-US", "Import" } } },
            { "refresh", new() { { "zh-CN", "刷新" }, { "en-US", "Refresh" } } },
            { "back", new() { { "zh-CN", "返回" }, { "en-US", "Back" } } },
            { "close", new() { { "zh-CN", "关闭" }, { "en-US", "Close" } } },

            // 状态
            { "status", new() { { "zh-CN", "状态" }, { "en-US", "Status" } } },
            { "enable", new() { { "zh-CN", "启用" }, { "en-US", "Enable" } } },
            { "disable", new() { { "zh-CN", "停用" }, { "en-US", "Disable" } } },
            { "enabled", new() { { "zh-CN", "已启用" }, { "en-US", "Enabled" } } },
            { "disabled", new() { { "zh-CN", "已停用" }, { "en-US", "Disabled" } } },
            { "normal", new() { { "zh-CN", "正常" }, { "en-US", "Normal" } } },
            { "success", new() { { "zh-CN", "成功" }, { "en-US", "Success" } } },
            { "failed", new() { { "zh-CN", "失败" }, { "en-US", "Failed" } } },
            { "error", new() { { "zh-CN", "错误" }, { "en-US", "Error" } } },
            { "warning", new() { { "zh-CN", "警告" }, { "en-US", "Warning" } } },
            { "info", new() { { "zh-CN", "信息" }, { "en-US", "Info" } } },
            { "yes", new() { { "zh-CN", "是" }, { "en-US", "Yes" } } },
            { "no", new() { { "zh-CN", "否" }, { "en-US", "No" } } },
            { "login_success", new() { { "zh-CN", "登录成功" }, { "en-US", "Login Success" } } },
            { "login_failed", new() { { "zh-CN", "登录失败" }, { "en-US", "Login Failed" } } },
            { "login_expired", new() { { "zh-CN", "登录已过期" }, { "en-US", "Login Expired" } } },

            // 通用字段
            { "id", new() { { "zh-CN", "编号" }, { "en-US", "ID" } } },
            { "name", new() { { "zh-CN", "名称" }, { "en-US", "Name" } } },
            { "code", new() { { "zh-CN", "编码" }, { "en-US", "Code" } } },
            { "type", new() { { "zh-CN", "类型" }, { "en-US", "Type" } } },
            { "sort", new() { { "zh-CN", "排序" }, { "en-US", "Sort" } } },
            { "remark", new() { { "zh-CN", "备注" }, { "en-US", "Remark" } } },
            { "description", new() { { "zh-CN", "描述" }, { "en-US", "Description" } } },
            { "create_time", new() { { "zh-CN", "创建时间" }, { "en-US", "Create Time" } } },
            { "update_time", new() { { "zh-CN", "更新时间" }, { "en-US", "Update Time" } } },
            { "create_by", new() { { "zh-CN", "创建人" }, { "en-US", "Created By" } } },
            { "update_by", new() { { "zh-CN", "更新人" }, { "en-US", "Updated By" } } },
            { "operation", new() { { "zh-CN", "操作" }, { "en-US", "Operation" } } },
            { "action", new() { { "zh-CN", "动作" }, { "en-US", "Action" } } },

            // 提示信息
            { "pleaseSelect", new() { { "zh-CN", "请选择" }, { "en-US", "Please Select" } } },
            { "pleaseInput", new() { { "zh-CN", "请输入" }, { "en-US", "Please Input" } } },
            { "please_select", new() { { "zh-CN", "请选择" }, { "en-US", "Please select" } } },
            { "please_input", new() { { "zh-CN", "请输入" }, { "en-US", "Please input" } } },
            { "confirmDelete", new() { { "zh-CN", "确认删除？" }, { "en-US", "Confirm Delete?" } } },
            { "deleteSuccess", new() { { "zh-CN", "删除成功" }, { "en-US", "Delete Success" } } },
            { "deleteFailed", new() { { "zh-CN", "删除失败" }, { "en-US", "Delete Failed" } } },
            { "saveSuccess", new() { { "zh-CN", "保存成功" }, { "en-US", "Save Success" } } },
            { "saveFailed", new() { { "zh-CN", "保存失败" }, { "en-US", "Save Failed" } } },
            { "loadFailed", new() { { "zh-CN", "加载失败" }, { "en-US", "Load Failed" } } },
            { "operationSuccess", new() { { "zh-CN", "操作成功" }, { "en-US", "Operation Success" } } },
            { "operationFailed", new() { { "zh-CN", "操作失败" }, { "en-US", "Operation Failed" } } },

            // 分页
            { "total", new() { { "zh-CN", "共" }, { "en-US", "Total" } } },
            { "items", new() { { "zh-CN", "条" }, { "en-US", "Items" } } },
            { "page", new() { { "zh-CN", "页" }, { "en-US", "Page" } } },
            { "pageSize", new() { { "zh-CN", "每页条数" }, { "en-US", "Page Size" } } },

            // 验证错误
            { "format_error", new() { { "zh-CN", "格式错误" }, { "en-US", "format error" } } },
            { "length_error", new() { { "zh-CN", "长度错误" }, { "en-US", "length error" } } },
            { "length_range", new() { { "zh-CN", "长度范围" }, { "en-US", "Length range" } } },
            { "2_20", new() { { "zh-CN", "2-20个字符" }, { "en-US", "2-20 characters" } } },

            // 用户相关字段
            { "username", new() { { "zh-CN", "用户名" }, { "en-US", "username" } } },
            { "password", new() { { "zh-CN", "密码" }, { "en-US", "password" } } },
            { "nickname", new() { { "zh-CN", "昵称" }, { "en-US", "nickname" } } },
            { "email", new() { { "zh-CN", "邮箱" }, { "en-US", "email" } } },
            { "phone", new() { { "zh-CN", "手机号" }, { "en-US", "phone" } } },
            { "dept", new() { { "zh-CN", "部门" }, { "en-US", "dept" } } },
            { "parent_dept", new() { { "zh-CN", "上级部门" }, { "en-US", "parent dept" } } },
            { "post", new() { { "zh-CN", "岗位" }, { "en-US", "post" } } },
            { "role", new() { { "zh-CN", "角色" }, { "en-US", "role" } } },

            // 字典相关
            { "dict_type", new() { { "zh-CN", "字典类型" }, { "en-US", "dict type" } } },
            { "dict_name", new() { { "zh-CN", "字典名称" }, { "en-US", "dict name" } } },
            { "label", new() { { "zh-CN", "标签" }, { "en-US", "label" } } },
            { "value", new() { { "zh-CN", "值" }, { "en-US", "value" } } },
            { "lang_key", new() { { "zh-CN", "语言键" }, { "en-US", "language key" } } },

            // 租户相关
            { "db_type", new() { { "zh-CN", "数据库类型" }, { "en-US", "database type" } } },
            { "db_connection", new() { { "zh-CN", "数据库连接" }, { "en-US", "database connection" } } },
            { "expire_time", new() { { "zh-CN", "过期时间" }, { "en-US", "expire time" } } },
            { "isolation", new() { { "zh-CN", "隔离方式" }, { "en-US", "isolation" } } },
            { "isolation_type", new() { { "zh-CN", "隔离类型" }, { "en-US", "isolation type" } } },
            { "isolation_physical", new() { { "zh-CN", "物理隔离" }, { "en-US", "Physical Isolation" } } },
            { "isolation_logical", new() { { "zh-CN", "逻辑隔离" }, { "en-US", "Logical Isolation" } } },

            // 数据权限范围
            { "data_scope_all", new() { { "zh-CN", "全部数据权限" }, { "en-US", "All Data" } } },
            { "data_scope_dept", new() { { "zh-CN", "本部门数据权限" }, { "en-US", "Department Data" } } },
            { "data_scope_self", new() { { "zh-CN", "仅本人数据权限" }, { "en-US", "Self Data Only" } } },

            // 项目相关
            { "project_name", new() { { "zh-CN", "项目名称" }, { "en-US", "project name" } } },
            { "display_name", new() { { "zh-CN", "显示名称" }, { "en-US", "display name" } } },
            { "connection_string", new() { { "zh-CN", "连接字符串" }, { "en-US", "connection string" } } },
            { "project_path", new() { { "zh-CN", "项目路径" }, { "en-US", "project path" } } },
            { "database_type", new() { { "zh-CN", "数据库类型" }, { "en-US", "database type" } } },

            // 菜单相关字段
            { "menu_name", new() { { "zh-CN", "菜单名称" }, { "en-US", "menu name" } } },
            { "icon", new() { { "zh-CN", "图标" }, { "en-US", "icon" } } },
            { "order_num", new() { { "zh-CN", "排序" }, { "en-US", "order" } } },
            { "perms", new() { { "zh-CN", "权限标识" }, { "en-US", "permission" } } },
            { "path", new() { { "zh-CN", "路由地址" }, { "en-US", "path" } } },
            { "component", new() { { "zh-CN", "组件路径" }, { "en-US", "component" } } },
            { "menu_type", new() { { "zh-CN", "菜单类型" }, { "en-US", "menu type" } } },
            { "directory", new() { { "zh-CN", "目录" }, { "en-US", "Directory" } } },
            { "menu", new() { { "zh-CN", "菜单" }, { "en-US", "Menu" } } },
            { "button", new() { { "zh-CN", "按钮" }, { "en-US", "Button" } } },

            // 语言名称
            { "chinese", new() { { "zh-CN", "简体中文" }, { "en-US", "Chinese" } } },
            { "english", new() { { "zh-CN", "英语" }, { "en-US", "English" } } },

            // 操作日志相关
            { "title", new() { { "zh-CN", "标题" }, { "en-US", "title" } } },
            { "oper_name", new() { { "zh-CN", "操作人员" }, { "en-US", "operator" } } },
            { "business_type", new() { { "zh-CN", "业务类型" }, { "en-US", "business type" } } },
            { "request_method", new() { { "zh-CN", "请求方式" }, { "en-US", "request method" } } },
            { "oper_ip", new() { { "zh-CN", "操作IP" }, { "en-US", "IP address" } } },
            { "oper_location", new() { { "zh-CN", "操作地点" }, { "en-US", "location" } } },
            { "elapsed", new() { { "zh-CN", "耗时" }, { "en-US", "elapsed" } } },
            { "oper_time", new() { { "zh-CN", "操作时间" }, { "en-US", "operation time" } } },
            { "clear_all", new() { { "zh-CN", "清空全部" }, { "en-US", "Clear All" } } },
            { "clear_all_confirm", new() { { "zh-CN", "确定要清空所有操作日志吗？" }, { "en-US", "Are you sure you want to clear all operation logs?" } } },

            // 业务类型
            { "business_other", new() { { "zh-CN", "其他" }, { "en-US", "Other" } } },
            { "business_insert", new() { { "zh-CN", "新增" }, { "en-US", "Insert" } } },
            { "business_update", new() { { "zh-CN", "修改" }, { "en-US", "Update" } } },
            { "business_delete", new() { { "zh-CN", "删除" }, { "en-US", "Delete" } } },
            { "business_grant", new() { { "zh-CN", "授权" }, { "en-US", "Grant" } } },
            { "business_export", new() { { "zh-CN", "导出" }, { "en-US", "Export" } } },
            { "business_import", new() { { "zh-CN", "导入" }, { "en-US", "Import" } } },
            { "business_force", new() { { "zh-CN", "强退" }, { "en-US", "Force Logout" } } },
            { "business_gencode", new() { { "zh-CN", "生成代码" }, { "en-US", "Generate Code" } } },
            { "business_clean", new() { { "zh-CN", "清空数据" }, { "en-US", "Clean Data" } } },

            // 在线用户相关
            { "online_user", new() { { "zh-CN", "在线用户" }, { "en-US", "Online Users" } } },
            { "user_id", new() { { "zh-CN", "用户ID" }, { "en-US", "User ID" } } },
            { "connection_id", new() { { "zh-CN", "连接ID" }, { "en-US", "Connection ID" } } },
            { "connect_time", new() { { "zh-CN", "连接时间" }, { "en-US", "Connect Time" } } },
            { "last_active_time", new() { { "zh-CN", "最后活跃时间" }, { "en-US", "Last Active Time" } } },
            { "force_offline", new() { { "zh-CN", "强制下线" }, { "en-US", "Force Offline" } } },
            { "force_offline_confirm", new() { { "zh-CN", "确定要强制用户 {name} 下线吗？" }, { "en-US", "Are you sure you want to force user {name} offline?" } } },
            { "load_online_failed", new() { { "zh-CN", "加载在线用户失败" }, { "en-US", "Failed to load online users" } } },
            { "action_success", new() { { "zh-CN", "操作成功" }, { "en-US", "Action Success" } } },
            { "action_failed", new() { { "zh-CN", "操作失败" }, { "en-US", "Action Failed" } } },

            // 任务管理相关
            { "task_name", new() { { "zh-CN", "任务名称" }, { "en-US", "task name" } } },
            { "task_id", new() { { "zh-CN", "任务ID" }, { "en-US", "Task ID" } } },
            { "job_group", new() { { "zh-CN", "任务组名" }, { "en-US", "job group" } } },
            { "task_type", new() { { "zh-CN", "任务类型" }, { "en-US", "task type" } } },
            { "task_type_assembly", new() { { "zh-CN", "程序集" }, { "en-US", "Assembly" } } },
            { "task_type_http", new() { { "zh-CN", "HTTP请求" }, { "en-US", "HTTP" } } },
            { "task_type_sql", new() { { "zh-CN", "SQL脚本" }, { "en-US", "SQL" } } },
            { "invoke_target", new() { { "zh-CN", "调用目标" }, { "en-US", "invoke target" } } },
            { "cron_expression", new() { { "zh-CN", "Cron表达式" }, { "en-US", "cron expression" } } },
            { "run_times", new() { { "zh-CN", "执行次数" }, { "en-US", "run times" } } },
            { "paused", new() { { "zh-CN", "暂停" }, { "en-US", "Paused" } } },
            { "run", new() { { "zh-CN", "执行" }, { "en-US", "Run" } } },
            { "log", new() { { "zh-CN", "日志" }, { "en-US", "Log" } } },
            { "run_task_confirm", new() { { "zh-CN", "确定要立即执行该任务吗？" }, { "en-US", "Are you sure you want to run this task now?" } } },
            { "run_success", new() { { "zh-CN", "执行成功" }, { "en-US", "Run Success" } } },
            { "run_failed", new() { { "zh-CN", "执行失败" }, { "en-US", "Run Failed" } } },
            { "query_failed", new() { { "zh-CN", "查询失败" }, { "en-US", "Query Failed" } } },
            { "job_message", new() { { "zh-CN", "执行消息" }, { "en-US", "job message" } } },

            // 项目管理相关
            { "project_type", new() { { "zh-CN", "项目类型" }, { "en-US", "project type" } } },
            { "server_version", new() { { "zh-CN", "服务端版本" }, { "en-US", "Server Version" } } },
            { "client_version", new() { { "zh-CN", "客户端版本" }, { "en-US", "Client Version" } } },
            { "mysql", new() { { "zh-CN", "MySQL" }, { "en-US", "MySQL" } } },
            { "sqlserver", new() { { "zh-CN", "SQL Server" }, { "en-US", "SQL Server" } } },
            { "postgresql", new() { { "zh-CN", "PostgreSQL" }, { "en-US", "PostgreSQL" } } },
            { "sqlite", new() { { "zh-CN", "SQLite" }, { "en-US", "SQLite" } } },
            { "oracle", new() { { "zh-CN", "Oracle" }, { "en-US", "Oracle" } } },
            { "not_initialized", new() { { "zh-CN", "未初始化" }, { "en-US", "Not Initialized" } } },
            { "initialized", new() { { "zh-CN", "已初始化" }, { "en-US", "Initialized" } } },
            { "running", new() { { "zh-CN", "运行中" }, { "en-US", "Running" } } },
            { "stopped", new() { { "zh-CN", "已停止" }, { "en-US", "Stopped" } } },
            { "initialize_failed", new() { { "zh-CN", "初始化失败" }, { "en-US", "Initialize Failed" } } },
            { "initialize", new() { { "zh-CN", "初始化" }, { "en-US", "Initialize" } } },
            { "start", new() { { "zh-CN", "启动" }, { "en-US", "Start" } } },
            { "stop", new() { { "zh-CN", "停止" }, { "en-US", "Stop" } } },
            { "actions", new() { { "zh-CN", "操作" }, { "en-US", "Actions" } } },
            { "add_project", new() { { "zh-CN", "新增项目" }, { "en-US", "Add Project" } } },
            { "project_name_tip", new() { { "zh-CN", "项目名称必须以大写字母开头，只能包含字母和数字" }, { "en-US", "Project name must start with uppercase letter and contain only letters and numbers" } } },
            { "display_name_en", new() { { "zh-CN", "显示名称(英文)" }, { "en-US", "display name (English)" } } },
            { "project_description", new() { { "zh-CN", "项目描述" }, { "en-US", "project description" } } },
            { "description_en", new() { { "zh-CN", "项目描述(英文)" }, { "en-US", "project description (English)" } } },
            { "project_type_server", new() { { "zh-CN", "服务端项目" }, { "en-US", "Server Project" } } },
            { "project_type_wpf", new() { { "zh-CN", "WPF客户端项目" }, { "en-US", "WPF Client Project" } } },
            { "db_mysql", new() { { "zh-CN", "MySQL" }, { "en-US", "MySQL" } } },
            { "db_sqlserver", new() { { "zh-CN", "SQL Server" }, { "en-US", "SQL Server" } } },
            { "db_postgresql", new() { { "zh-CN", "PostgreSQL" }, { "en-US", "PostgreSQL" } } },
            { "db_sqlite", new() { { "zh-CN", "SQLite" }, { "en-US", "SQLite" } } },
            { "db_oracle", new() { { "zh-CN", "Oracle" }, { "en-US", "Oracle" } } },
            { "connection_string_tip", new() { { "zh-CN", "数据库连接字符串" }, { "en-US", "Database connection string" } } },
            { "project_path_tip", new() { { "zh-CN", "项目生成路径" }, { "en-US", "Project generation path" } } },
            { "logo_path", new() { { "zh-CN", "Logo路径" }, { "en-US", "logo path" } } },
            { "frontend_port", new() { { "zh-CN", "前端端口" }, { "en-US", "frontend port" } } },
            { "backend_port", new() { { "zh-CN", "后端端口" }, { "en-US", "backend port" } } },
            { "create_success", new() { { "zh-CN", "创建成功" }, { "en-US", "Create Success" } } },
            { "create_failed", new() { { "zh-CN", "创建失败" }, { "en-US", "Create Failed" } } },

            // TabsView 右键菜单
            { "closeOthers", new() { { "zh-CN", "关闭其他" }, { "en-US", "Close Others" } } },
            { "closeAll", new() { { "zh-CN", "关闭所有" }, { "en-US", "Close All" } } },

            // 模板生成相关
            { "generate_template", new() { { "zh-CN", "生成模板" }, { "en-US", "Generate Template" } } },
            { "confirm_generate_template", new() { { "zh-CN", "确定要生成项目模板吗？" }, { "en-US", "Are you sure you want to generate project template?" } } },
            { "generate_template_success", new() { { "zh-CN", "模板生成成功" }, { "en-US", "Template Generated Successfully" } } },
            { "generate_template_failed", new() { { "zh-CN", "模板生成失败" }, { "en-US", "Template Generation Failed" } } },
            { "generating_template", new() { { "zh-CN", "正在生成模板..." }, { "en-US", "Generating template..." } } },
        };
    }
}
