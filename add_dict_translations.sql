-- 添加字典管理相关的翻译键

-- 获取语言ID
DECLARE @zhCN_Id BIGINT = (SELECT Id FROM SysLang WHERE LangCode = 'zh-CN');
DECLARE @enUS_Id BIGINT = (SELECT Id FROM SysLang WHERE LangCode = 'en-US');

-- 删除已存在的翻译（如果有）
DELETE FROM SysLangText WHERE LangKey IN ('dictName', 'dictType', 'add_dict_type', 'add_dict_data', 'please_input_dictName', 'please_input_dictType');

-- 插入中文翻译
INSERT INTO SysLangText (LangCode, LangKey, LangValue, CreateTime) VALUES
('zh-CN', 'dictName', '字典名称', GETDATE()),
('zh-CN', 'dictType', '字典类型', GETDATE()),
('zh-CN', 'add_dict_type', '添加字典类型', GETDATE()),
('zh-CN', 'add_dict_data', '添加字典数据', GETDATE()),
('zh-CN', 'please_input_dictName', '请输入字典名称', GETDATE()),
('zh-CN', 'please_input_dictType', '请输入字典类型', GETDATE());

-- 插入英文翻译
INSERT INTO SysLangText (LangCode, LangKey, LangValue, CreateTime) VALUES
('en-US', 'dictName', 'Dictionary Name', GETDATE()),
('en-US', 'dictType', 'Dictionary Type', GETDATE()),
('en-US', 'add_dict_type', 'Add Dictionary Type', GETDATE()),
('en-US', 'add_dict_data', 'Add Dictionary Data', GETDATE()),
('en-US', 'please_input_dictName', 'Please enter dictionary name', GETDATE()),
('en-US', 'please_input_dictType', 'Please enter dictionary type', GETDATE());

SELECT '翻译添加完成' AS Result;
