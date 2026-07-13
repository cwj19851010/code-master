-- 添加按钮相关的翻译键（不带 common. 前缀）
-- 中文翻译 (lang_code = 'zh-CN')
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
('zh-CN', 'add', '新增', 'button', GETDATE(), 0),
('zh-CN', 'edit', '编辑', 'button', GETDATE(), 0),
('zh-CN', 'delete', '删除', 'button', GETDATE(), 0),
('zh-CN', 'view', '详情', 'button', GETDATE(), 0),
('zh-CN', 'export', '导出', 'button', GETDATE(), 0),
('zh-CN', 'import', '导入', 'button', GETDATE(), 0),
('zh-CN', 'search', '搜索', 'button', GETDATE(), 0),
('zh-CN', 'reset', '重置', 'button', GETDATE(), 0),
('zh-CN', 'save', '保存', 'button', GETDATE(), 0),
('zh-CN', 'cancel', '取消', 'button', GETDATE(), 0),
('zh-CN', 'back', '返回', 'button', GETDATE(), 0),
('zh-CN', 'confirm', '确认', 'button', GETDATE(), 0),
('zh-CN', 'submit', '提交', 'button', GETDATE(), 0),
('zh-CN', 'refresh', '刷新', 'button', GETDATE(), 0),
('zh-CN', 'close', '关闭', 'button', GETDATE(), 0),
('zh-CN', 'closeOthers', '关闭其他', 'button', GETDATE(), 0),
('zh-CN', 'closeAll', '关闭所有', 'button', GETDATE(), 0),
('zh-CN', 'operlog', '操作日志', 'button', GETDATE(), 0),
('zh-CN', 'batch_delete', '批量删除', 'button', GETDATE(), 0);

-- 英文翻译 (lang_code = 'en-US')
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
('en-US', 'add', 'Add', 'button', GETDATE(), 0),
('en-US', 'edit', 'Edit', 'button', GETDATE(), 0),
('en-US', 'delete', 'Delete', 'button', GETDATE(), 0),
('en-US', 'view', 'View', 'button', GETDATE(), 0),
('en-US', 'export', 'Export', 'button', GETDATE(), 0),
('en-US', 'import', 'Import', 'button', GETDATE(), 0),
('en-US', 'search', 'Search', 'button', GETDATE(), 0),
('en-US', 'reset', 'Reset', 'button', GETDATE(), 0),
('en-US', 'save', 'Save', 'button', GETDATE(), 0),
('en-US', 'cancel', 'Cancel', 'button', GETDATE(), 0),
('en-US', 'back', 'Back', 'button', GETDATE(), 0),
('en-US', 'confirm', 'Confirm', 'button', GETDATE(), 0),
('en-US', 'submit', 'Submit', 'button', GETDATE(), 0),
('en-US', 'refresh', 'Refresh', 'button', GETDATE(), 0),
('en-US', 'close', 'Close', 'button', GETDATE(), 0),
('en-US', 'closeOthers', 'Close Others', 'button', GETDATE(), 0),
('en-US', 'closeAll', 'Close All', 'button', GETDATE(), 0),
('en-US', 'operlog', 'Operation Log', 'button', GETDATE(), 0),
('en-US', 'batch_delete', 'Batch Delete', 'button', GETDATE(), 0);

-- 提示信息翻译
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
('zh-CN', 'prompt', '提示', 'message', GETDATE(), 0),
('zh-CN', 'delete_confirm', '确定要删除这条记录吗？', 'message', GETDATE(), 0),
('zh-CN', 'batch_delete_confirm', '确定要删除选中的 {count} 条记录吗？', 'message', GETDATE(), 0),
('zh-CN', 'delete_success', '删除成功', 'message', GETDATE(), 0),
('zh-CN', 'batch_delete_success', '批量删除成功', 'message', GETDATE(), 0),
('zh-CN', 'delete_failed', '删除失败', 'message', GETDATE(), 0),
('zh-CN', 'please_select_delete', '请选择要删除的记录', 'message', GETDATE(), 0),
('zh-CN', 'operation_success', '操作成功', 'message', GETDATE(), 0),
('zh-CN', 'operation_failed', '操作失败', 'message', GETDATE(), 0);

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
('en-US', 'prompt', 'Prompt', 'message', GETDATE(), 0),
('en-US', 'delete_confirm', 'Are you sure you want to delete this record?', 'message', GETDATE(), 0),
('en-US', 'batch_delete_confirm', 'Are you sure you want to delete {count} selected records?', 'message', GETDATE(), 0),
('en-US', 'delete_success', 'Delete successfully', 'message', GETDATE(), 0),
('en-US', 'batch_delete_success', 'Batch delete successfully', 'message', GETDATE(), 0),
('en-US', 'delete_failed', 'Delete failed', 'message', GETDATE(), 0),
('en-US', 'please_select_delete', 'Please select records to delete', 'message', GETDATE(), 0),
('en-US', 'operation_success', 'Operation successfully', 'message', GETDATE(), 0),
('en-US', 'operation_failed', 'Operation failed', 'message', GETDATE(), 0);

PRINT '按钮和提示信息翻译添加完成！';
