-- 使用递增的 ID 添加翻译（从最大 ID + 1 开始）
DECLARE @BaseId BIGINT = 774733782380712;

INSERT INTO sys_lang_texts (id, lang_code, lang_key, lang_value, category, create_time, is_deleted) VALUES
(@BaseId, 'zh-CN', 'export', N'导出', 'button', GETDATE(), 0),
(@BaseId + 1, 'zh-CN', 'import', N'导入', 'button', GETDATE(), 0),
(@BaseId + 2, 'zh-CN', 'view', N'详情', 'button', GETDATE(), 0),
(@BaseId + 3, 'zh-CN', 'add', N'新增', 'button', GETDATE(), 0),
(@BaseId + 4, 'zh-CN', 'edit', N'编辑', 'button', GETDATE(), 0),
(@BaseId + 5, 'zh-CN', 'search', N'搜索', 'button', GETDATE(), 0),
(@BaseId + 6, 'zh-CN', 'reset', N'重置', 'button', GETDATE(), 0),
(@BaseId + 7, 'zh-CN', 'operlog', N'操作日志', 'button', GETDATE(), 0),
(@BaseId + 8, 'en-US', 'export', 'Export', 'button', GETDATE(), 0),
(@BaseId + 9, 'en-US', 'import', 'Import', 'button', GETDATE(), 0),
(@BaseId + 10, 'en-US', 'view', 'View', 'button', GETDATE(), 0),
(@BaseId + 11, 'en-US', 'add', 'Add', 'button', GETDATE(), 0),
(@BaseId + 12, 'en-US', 'edit', 'Edit', 'button', GETDATE(), 0),
(@BaseId + 13, 'en-US', 'search', 'Search', 'button', GETDATE(), 0),
(@BaseId + 14, 'en-US', 'reset', 'Reset', 'button', GETDATE(), 0),
(@BaseId + 15, 'en-US', 'operlog', 'Operation Log', 'button', GETDATE(), 0);

SELECT @@ROWCOUNT AS 'Rows Inserted';
