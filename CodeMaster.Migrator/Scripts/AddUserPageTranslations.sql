-- 添加用户管理页面的所有翻译键
-- 中文翻译 (lang_code = 'zh-CN')
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
-- 字段标签
('zh-CN', 'username', '用户名', 'field', GETDATE(), 0),
('zh-CN', 'nickname', '昵称', 'field', GETDATE(), 0),
('zh-CN', 'email', '邮箱', 'field', GETDATE(), 0),
('zh-CN', 'phone', '手机号', 'field', GETDATE(), 0),
('zh-CN', 'status', '状态', 'field', GETDATE(), 0),
('zh-CN', 'create_time', '创建时间', 'field', GETDATE(), 0),
('zh-CN', 'action', '操作', 'field', GETDATE(), 0),

-- 占位符
('zh-CN', 'please_input_username', '请输入用户名', 'placeholder', GETDATE(), 0),
('zh-CN', 'please_input_phone', '请输入手机号', 'placeholder', GETDATE(), 0),
('zh-CN', 'please_select_status', '请选择状态', 'placeholder', GETDATE(), 0),

-- 状态选项
('zh-CN', 'status_normal', '正常', 'option', GETDATE(), 0),
('zh-CN', 'status_disabled', '停用', 'option', GETDATE(), 0),

-- 提示信息
('zh-CN', 'query_failed', '查询失败', 'message', GETDATE(), 0),
('zh-CN', 'batchDelete', '批量删除', 'button', GETDATE(), 0);

-- 英文翻译 (lang_code = 'en-US')
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
-- 字段标签
('en-US', 'username', 'Username', 'field', GETDATE(), 0),
('en-US', 'nickname', 'Nickname', 'field', GETDATE(), 0),
('en-US', 'email', 'Email', 'field', GETDATE(), 0),
('en-US', 'phone', 'Phone', 'field', GETDATE(), 0),
('en-US', 'status', 'Status', 'field', GETDATE(), 0),
('en-US', 'create_time', 'Create Time', 'field', GETDATE(), 0),
('en-US', 'action', 'Action', 'field', GETDATE(), 0),

-- 占位符
('en-US', 'please_input_username', 'Please input username', 'placeholder', GETDATE(), 0),
('en-US', 'please_input_phone', 'Please input phone', 'placeholder', GETDATE(), 0),
('en-US', 'please_select_status', 'Please select status', 'placeholder', GETDATE(), 0),

-- 状态选项
('en-US', 'status_normal', 'Normal', 'option', GETDATE(), 0),
('en-US', 'status_disabled', 'Disabled', 'option', GETDATE(), 0),

-- 提示信息
('en-US', 'query_failed', 'Query failed', 'message', GETDATE(), 0),
('en-US', 'batchDelete', 'Batch Delete', 'button', GETDATE(), 0);

PRINT '用户管理页面翻译添加完成！';
