-- 添加日志详情页的隐藏路由和翻译

-- 1. 添加操作日志详情路由
INSERT INTO sys_menus (id, parent_id, menu_name, path, component, menu_type, visible, status, perms, icon, sort_order, is_cache, create_time, menu_scope)
VALUES (774733781725270, 774733781725253, '操作日志详情', '/monitor/operlog/detail', 'system/operlog/detail', 'C', 1, 0, 'monitor:operlog:view', '', 0, 0, GETDATE(), 2);

-- 2. 添加登录日志详情路由
INSERT INTO sys_menus (id, parent_id, menu_name, path, component, menu_type, visible, status, perms, icon, sort_order, is_cache, create_time, menu_scope)
VALUES (774733781725271, 774733781725253, '登录日志详情', '/monitor/loginlog/detail', 'system/loginlog/detail', 'C', 1, 0, 'monitor:loginlog:view', '', 0, 0, GETDATE(), 2);

-- 3. 添加任务日志详情路由
INSERT INTO sys_menus (id, parent_id, menu_name, path, component, menu_type, visible, status, perms, icon, sort_order, is_cache, create_time, menu_scope)
VALUES (774733781725272, 774733781725253, '任务日志详情', '/monitor/tasklog/detail', 'system/tasklog/detail', 'C', 1, 0, 'monitor:tasklog:view', '', 0, 0, GETDATE(), 2);

-- 4. 为 admin 角色分配这些路由权限
INSERT INTO sys_role_menus (role_id, menu_id) VALUES (1, 774733781725270);
INSERT INTO sys_role_menus (role_id, menu_id) VALUES (1, 774733781725271);
INSERT INTO sys_role_menus (role_id, menu_id) VALUES (1, 774733781725272);

-- 5. 添加翻译键
INSERT INTO sys_lang_texts (lang_key, lang_code, lang_value) VALUES
('operlog_detail', 'zh-CN', '操作日志详情'),
('operlog_detail', 'en-US', 'Operation Log Detail'),
('loginlog_detail', 'zh-CN', '登录日志详情'),
('loginlog_detail', 'en-US', 'Login Log Detail'),
('tasklog_detail', 'zh-CN', '任务日志详情'),
('tasklog_detail', 'en-US', 'Task Log Detail'),
('log_id', 'zh-CN', '日志ID'),
('log_id', 'en-US', 'Log ID'),
('task_id', 'zh-CN', '任务ID'),
('task_id', 'en-US', 'Task ID'),
('task_name', 'zh-CN', '任务名称'),
('task_name', 'en-US', 'Task Name'),
('invoke_target', 'zh-CN', '调用目标'),
('invoke_target', 'en-US', 'Invoke Target'),
('job_message', 'zh-CN', '执行信息'),
('job_message', 'en-US', 'Job Message'),
('status_success', 'zh-CN', '成功'),
('status_success', 'en-US', 'Success'),
('status_failed', 'zh-CN', '失败'),
('status_failed', 'en-US', 'Failed'),
('oper_location', 'zh-CN', '操作地点'),
('oper_location', 'en-US', 'Operation Location'),
('login_location', 'zh-CN', '登录地点'),
('login_location', 'en-US', 'Login Location'),
('oper_url', 'zh-CN', '请求URL'),
('oper_url', 'en-US', 'Request URL'),
('oper_param', 'zh-CN', '请求参数'),
('oper_param', 'en-US', 'Request Parameters'),
('json_result', 'zh-CN', '返回结果'),
('json_result', 'en-US', 'JSON Result'),
('error_msg', 'zh-CN', '错误信息'),
('error_msg', 'en-US', 'Error Message'),
('method', 'zh-CN', '方法名称'),
('method', 'en-US', 'Method'),
('business_type_other', 'zh-CN', '其他'),
('business_type_other', 'en-US', 'Other'),
('business_type_insert', 'zh-CN', '新增'),
('business_type_insert', 'en-US', 'Insert'),
('business_type_update', 'zh-CN', '修改'),
('business_type_update', 'en-US', 'Update'),
('business_type_delete', 'zh-CN', '删除'),
('business_type_delete', 'en-US', 'Delete'),
('business_type_grant', 'zh-CN', '授权'),
('business_type_grant', 'en-US', 'Grant'),
('business_type_export', 'zh-CN', '导出'),
('business_type_export', 'en-US', 'Export'),
('business_type_import', 'zh-CN', '导入'),
('business_type_import', 'en-US', 'Import'),
('business_type_force', 'zh-CN', '强退'),
('business_type_force', 'en-US', 'Force Logout'),
('business_type_clean', 'zh-CN', '清空'),
('business_type_clean', 'en-US', 'Clean'),
('invalid_id', 'zh-CN', '无效的ID'),
('invalid_id', 'en-US', 'Invalid ID'),
('fetch_failed', 'zh-CN', '获取数据失败'),
('fetch_failed', 'en-US', 'Failed to fetch data');
