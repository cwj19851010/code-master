-- 优化菜单结构脚本
-- 1. 调整现有菜单的目录层级
-- 2. 为每个功能模块添加新增、编辑、详情的隐藏路由

USE CodeMasterDb;
GO

-- 首先，创建监控中心目录（用于日志管理）
DECLARE @MonitorId BIGINT = 774733781725254;
DECLARE @SystemId BIGINT = 774733781725253;

-- 检查监控中心目录是否存在
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE id = @MonitorId)
BEGIN
    INSERT INTO sys_menus (id, parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@MonitorId, NULL, '系统监控', 'monitor', 'M', '/monitor', 'Layout', 'Monitor', 2, 0, 0, 0, 0, NULL, GETDATE(), GETDATE(), 0, 2);
    PRINT '创建监控中心目录';
END

-- 将操作日志和登录日志移动到监控中心
UPDATE sys_menus SET parent_id = @MonitorId, path = 'operlog', order_num = 1 WHERE id = 774733781762125;
UPDATE sys_menus SET parent_id = @MonitorId, path = 'loginlog', order_num = 2 WHERE id = 774733781762126;
PRINT '移动日志管理到监控中心';

-- 将任务管理移动到监控中心
UPDATE sys_menus SET parent_id = @MonitorId, path = 'task', order_num = 3 WHERE id = 1200;
UPDATE sys_menus SET parent_id = @MonitorId, path = 'tasklog', order_num = 4 WHERE id = 1201;
PRINT '移动任务管理到监控中心';

-- 为用户管理添加新增、编辑、详情路由（隐藏路由）
DECLARE @UserListId BIGINT = 774733781762117;

-- 用户新增页面
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'user/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增用户', 'user.add', 'C', 'user/add', 'system/user/add', NULL, 101, 1, 0, 0, 1, 'system:user:create', GETDATE(), GETDATE(), 0, 2);
    PRINT '创建用户新增页面';
END

-- 用户编辑页面
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'user/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑用户', 'user.edit', 'C', 'user/edit', 'system/user/edit', NULL, 102, 1, 0, 0, 0, 'system:user:update', GETDATE(), GETDATE(), 0, 2);
    PRINT '创建用户编辑页面';
END

-- 用户详情页面
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'user/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '用户详情', 'user.detail', 'C', 'user/detail', 'system/user/detail', NULL, 103, 1, 0, 0, 0, 'system:user:view', GETDATE(), GETDATE(), 0, 2);
    PRINT '创建用户详情页面';
END

-- 为角色管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'role/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增角色', 'role.add', 'C', 'role/add', 'system/role/add', NULL, 201, 1, 0, 0, 1, 'system:role:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'role/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑角色', 'role.edit', 'C', 'role/edit', 'system/role/edit', NULL, 202, 1, 0, 0, 0, 'system:role:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'role/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '角色详情', 'role.detail', 'C', 'role/detail', 'system/role/detail', NULL, 203, 1, 0, 0, 0, 'system:role:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为部门管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dept/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增部门', 'dept.add', 'C', 'dept/add', 'system/dept/add', NULL, 301, 1, 0, 0, 1, 'system:dept:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dept/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑部门', 'dept.edit', 'C', 'dept/edit', 'system/dept/edit', NULL, 302, 1, 0, 0, 0, 'system:dept:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dept/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '部门详情', 'dept.detail', 'C', 'dept/detail', 'system/dept/detail', NULL, 303, 1, 0, 0, 0, 'system:dept:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为菜单管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'menu/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增菜单', 'menu.add', 'C', 'menu/add', 'system/menu/add', NULL, 401, 1, 0, 0, 1, 'system:menu:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'menu/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑菜单', 'menu.edit', 'C', 'menu/edit', 'system/menu/edit', NULL, 402, 1, 0, 0, 0, 'system:menu:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'menu/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '菜单详情', 'menu.detail', 'C', 'menu/detail', 'system/menu/detail', NULL, 403, 1, 0, 0, 0, 'system:menu:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为租户管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'tenant/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增租户', 'tenant.add', 'C', 'tenant/add', 'system/tenant/add', NULL, 501, 1, 0, 0, 1, 'system:tenant:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'tenant/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑租户', 'tenant.edit', 'C', 'tenant/edit', 'system/tenant/edit', NULL, 502, 1, 0, 0, 0, 'system:tenant:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'tenant/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '租户详情', 'tenant.detail', 'C', 'tenant/detail', 'system/tenant/detail', NULL, 503, 1, 0, 0, 0, 'system:tenant:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为岗位管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'post/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增岗位', 'post.add', 'C', 'post/add', 'system/post/add', NULL, 601, 1, 0, 0, 1, 'system:post:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'post/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑岗位', 'post.edit', 'C', 'post/edit', 'system/post/edit', NULL, 602, 1, 0, 0, 0, 'system:post:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'post/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '岗位详情', 'post.detail', 'C', 'post/detail', 'system/post/detail', NULL, 603, 1, 0, 0, 0, 'system:post:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为字典管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dict/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增字典', 'dict.add', 'C', 'dict/add', 'system/dict/add', NULL, 701, 1, 0, 0, 1, 'system:dict:type:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dict/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑字典', 'dict.edit', 'C', 'dict/edit', 'system/dict/edit', NULL, 702, 1, 0, 0, 0, 'system:dict:type:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'dict/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '字典详情', 'dict.detail', 'C', 'dict/detail', 'system/dict/detail', NULL, 703, 1, 0, 0, 0, 'system:dict:type:view', GETDATE(), GETDATE(), 0, 2);
END

-- 为语言管理添加新增、编辑、详情路由
IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'lang/add' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '新增语言', 'lang.add', 'C', 'lang/add', 'system/lang/add', NULL, 801, 1, 0, 0, 1, 'system:lang:create', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'lang/edit' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '编辑语言', 'lang.edit', 'C', 'lang/edit', 'system/lang/edit', NULL, 802, 1, 0, 0, 0, 'system:lang:update', GETDATE(), GETDATE(), 0, 2);
END

IF NOT EXISTS (SELECT 1 FROM sys_menus WHERE path = 'lang/detail' AND parent_id = @SystemId)
BEGIN
    INSERT INTO sys_menus (parent_id, menu_name, title_key, menu_type, path, component, icon, order_num, visible, status, is_frame, is_cache, perms, create_time, update_time, is_deleted, menu_scope)
    VALUES (@SystemId, '语言详情', 'lang.detail', 'C', 'lang/detail', 'system/lang/detail', NULL, 803, 1, 0, 0, 0, 'system:lang:view', GETDATE(), GETDATE(), 0, 2);
END

PRINT '菜单结构优化完成！';
GO
