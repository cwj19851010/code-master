-- =============================================
-- 任务调度功能 - 数据初始化脚本
-- =============================================

-- 1. 添加任务管理菜单
DECLARE @TaskMenuId BIGINT = 1200;
DECLARE @TaskLogMenuId BIGINT = 1201;

-- 检查并插入任务管理菜单
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE id = @TaskMenuId)
BEGIN
    INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, path, component, perms, icon, order_num, visible, status, create_time, update_time)
    VALUES (@TaskMenuId, 1, 'system.menu.task', 'C', 'task', 'system/task/index', 'system:task:list', 'Timer', 7, '0', '0', GETDATE(), GETDATE());
    PRINT '任务管理菜单已添加';
END
ELSE
BEGIN
    PRINT '任务管理菜单已存在';
END

-- 检查并插入任务日志菜单
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE id = @TaskLogMenuId)
BEGIN
    INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, path, component, perms, icon, order_num, visible, status, create_time, update_time)
    VALUES (@TaskLogMenuId, 1, 'system.menu.tasklog', 'C', 'tasklog', 'system/tasklog/index', 'system:tasklog:list', 'Document', 8, '0', '0', GETDATE(), GETDATE());
    PRINT '任务日志菜单已添加';
END
ELSE
BEGIN
    PRINT '任务日志菜单已存在';
END

-- 2. 添加中文翻译
DECLARE @LangIdZh BIGINT;
SELECT @LangIdZh = id FROM sys_langs WHERE lang_code = 'zh-CN';

IF @LangIdZh IS NOT NULL
BEGIN
    -- 菜单翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.menu.task')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.menu.task', '任务管理', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.menu.tasklog')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.menu.tasklog', '任务日志', GETDATE(), GETDATE());
    END

    -- 任务类型翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.task.type.assembly')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.task.type.assembly', '程序集', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.task.type.http')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.task.type.http', 'HTTP请求', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.task.type.sql')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.task.type.sql', 'SQL脚本', GETDATE(), GETDATE());
    END

    -- 任务状态翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.task.status.normal')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.task.status.normal', '正常', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdZh AND lang_key = 'system.task.status.paused')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdZh, 'system.task.status.paused', '暂停', GETDATE(), GETDATE());
    END

    PRINT '✓ 中文翻译已添加';
END

-- 3. 添加英文翻译
DECLARE @LangIdEn BIGINT;
SELECT @LangIdEn = id FROM sys_langs WHERE lang_code = 'en-US';

IF @LangIdEn IS NOT NULL
BEGIN
    -- 菜单翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.menu.task')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.menu.task', 'Task Management', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.menu.tasklog')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.menu.tasklog', 'Task Log', GETDATE(), GETDATE());
    END

    -- 任务类型翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.task.type.assembly')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.task.type.assembly', 'Assembly', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.task.type.http')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.task.type.http', 'HTTP Request', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.task.type.sql')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.task.type.sql', 'SQL Script', GETDATE(), GETDATE());
    END

    -- 任务状态翻译
    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.task.status.normal')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.task.status.normal', 'Normal', GETDATE(), GETDATE());
    END

    IF NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_id = @LangIdEn AND lang_key = 'system.task.status.paused')
    BEGIN
        INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value, create_time, update_time)
        VALUES (@LangIdEn, 'system.task.status.paused', 'Paused', GETDATE(), GETDATE());
    END

    PRINT '✓ 英文翻译已添加';
END

-- 4. 添加任务类型字典
DECLARE @TaskTypeDictId BIGINT;
SELECT @TaskTypeDictId = id FROM sys_dict_types WHERE dict_type = 'sys_task_type';

IF @TaskTypeDictId IS NULL
BEGIN
    -- 生成新的字典类型ID
    SET @TaskTypeDictId = (SELECT ISNULL(MAX(id), 0) + 1 FROM sys_dict_types);

    INSERT INTO sys_dict_types (id, dict_name, dict_type, status, remark, create_time, update_time)
    VALUES (@TaskTypeDictId, '任务类型', 'sys_task_type', 0, '任务调度类型列表', GETDATE(), GETDATE());

    -- 添加字典数据
    INSERT INTO sys_dict_datas (dict_type_id, dict_label, dict_value, dict_sort, status, remark, create_time, update_time)
    VALUES
        (@TaskTypeDictId, '程序集', '0', 1, 0, '程序集任务', GETDATE(), GETDATE()),
        (@TaskTypeDictId, 'HTTP请求', '1', 2, 0, 'HTTP请求任务', GETDATE(), GETDATE()),
        (@TaskTypeDictId, 'SQL脚本', '2', 3, 0, 'SQL脚本任务', GETDATE(), GETDATE());

    PRINT '✓ 任务类型字典已添加';
END
ELSE
BEGIN
    PRINT '✓ 任务类型字典已存在';
END

-- 5. 添加任务状态字典
DECLARE @TaskStatusDictId BIGINT;
SELECT @TaskStatusDictId = id FROM sys_dict_types WHERE dict_type = 'sys_task_status';

IF @TaskStatusDictId IS NULL
BEGIN
    -- 生成新的字典类型ID
    SET @TaskStatusDictId = (SELECT ISNULL(MAX(id), 0) + 1 FROM sys_dict_types);

    INSERT INTO sys_dict_types (id, dict_name, dict_type, status, remark, create_time, update_time)
    VALUES (@TaskStatusDictId, '任务状态', 'sys_task_status', 0, '任务调度状态列表', GETDATE(), GETDATE());

    -- 添加字典数据
    INSERT INTO sys_dict_datas (dict_type_id, dict_label, dict_value, dict_sort, status, remark, create_time, update_time)
    VALUES
        (@TaskStatusDictId, '正常', '0', 1, 0, '正常状态', GETDATE(), GETDATE()),
        (@TaskStatusDictId, '暂停', '1', 2, 0, '暂停状态', GETDATE(), GETDATE());

    PRINT '✓ 任务状态字典已添加';
END
ELSE
BEGIN
    PRINT '✓ 任务状态字典已存在';
END

PRINT '';
PRINT '=============================================';
PRINT '任务调度功能数据初始化完成！';
PRINT '=============================================';
