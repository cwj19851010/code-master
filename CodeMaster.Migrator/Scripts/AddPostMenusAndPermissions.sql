-- 添加职位管理的隐藏路由和按钮权限

-- 1. 查找职位管理菜单ID和Admin角色ID
DECLARE @PostMenuId BIGINT
DECLARE @AdminRoleId BIGINT

SELECT @PostMenuId = id FROM sys_menus WHERE menu_name = N'职位管理' AND menu_type = 'C'
SELECT @AdminRoleId = id FROM sys_roles WHERE role_name = N'超级管理员'

PRINT N'职位管理菜单ID: ' + CAST(@PostMenuId AS NVARCHAR)
PRINT N'Admin角色ID: ' + CAST(@AdminRoleId AS NVARCHAR)

-- 2. 删除旧的隐藏路由（如果存在）
DELETE FROM sys_menus WHERE parent_id = @PostMenuId AND menu_type = 'M'

-- 3. 添加隐藏路由（新增/编辑/详情）
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, path, component, menu_type, visible, is_cache, perms, icon, order_num, create_time, remark, menu_scope)
VALUES
(774733781762158, @PostMenuId, N'新增职位', 'post_add', '/system/post/add', 'system/post/add', 'M', 0, 0, 'system:post:create', '', 1, GETDATE(), N'新增职位页面', 2),
(774733781762159, @PostMenuId, N'编辑职位', 'post_edit', '/system/post/edit/:id', 'system/post/edit', 'M', 0, 0, 'system:post:update', '', 2, GETDATE(), N'编辑职位页面', 2),
(774733781762160, @PostMenuId, N'职位详情', 'post_detail', '/system/post/detail/:id', 'system/post/detail', 'M', 0, 0, 'system:post:view', '', 3, GETDATE(), N'职位详情页面', 2)

-- 4. 添加按钮权限
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, menu_type, visible, perms, order_num, create_time, remark, menu_scope)
VALUES
(774733781762161, @PostMenuId, N'查看', 'view', 'F', 1, 'system:post:view', 1, GETDATE(), N'查看职位按钮', 2),
(774733781762162, @PostMenuId, N'新增', 'create', 'F', 1, 'system:post:create', 2, GETDATE(), N'新增职位按钮', 2),
(774733781762163, @PostMenuId, N'编辑', 'update', 'F', 1, 'system:post:update', 3, GETDATE(), N'编辑职位按钮', 2),
(774733781762164, @PostMenuId, N'删除', 'delete', 'F', 1, 'system:post:delete', 4, GETDATE(), N'删除职位按钮', 2)

-- 5. 为admin角色分配新增的菜单权限
DELETE FROM sys_role_menu WHERE role_id = @AdminRoleId AND menu_id IN (774733781762158, 774733781762159, 774733781762160, 774733781762161, 774733781762162, 774733781762163, 774733781762164)

INSERT INTO sys_role_menu (role_id, menu_id)
VALUES
(@AdminRoleId, 774733781762158),
(@AdminRoleId, 774733781762159),
(@AdminRoleId, 774733781762160),
(@AdminRoleId, 774733781762161),
(@AdminRoleId, 774733781762162),
(@AdminRoleId, 774733781762163),
(@AdminRoleId, 774733781762164)

-- 6. 添加翻译数据
DELETE FROM sys_lang_texts WHERE lang_key IN ('post_add', 'post_edit', 'post_detail')

INSERT INTO sys_lang_texts (id, lang_id, lang_key, lang_value, create_time)
SELECT
    NEXT VALUE FOR seq_sys_lang_texts,
    l.id,
    'post_add',
    CASE l.lang_code
        WHEN 'zh-CN' THEN N'新增职位'
        WHEN 'en-US' THEN 'Add Post'
    END,
    GETDATE()
FROM sys_langs l
WHERE l.lang_code IN ('zh-CN', 'en-US')

INSERT INTO sys_lang_texts (id, lang_id, lang_key, lang_value, create_time)
SELECT
    NEXT VALUE FOR seq_sys_lang_texts,
    l.id,
    'post_edit',
    CASE l.lang_code
        WHEN 'zh-CN' THEN N'编辑职位'
        WHEN 'en-US' THEN 'Edit Post'
    END,
    GETDATE()
FROM sys_langs l
WHERE l.lang_code IN ('zh-CN', 'en-US')

INSERT INTO sys_lang_texts (id, lang_id, lang_key, lang_value, create_time)
SELECT
    NEXT VALUE FOR seq_sys_lang_texts,
    l.id,
    'post_detail',
    CASE l.lang_code
        WHEN 'zh-CN' THEN N'职位详情'
        WHEN 'en-US' THEN 'Post Detail'
    END,
    GETDATE()
FROM sys_langs l
WHERE l.lang_code IN ('zh-CN', 'en-US')

PRINT N'职位管理菜单和权限配置完成！'
GO
