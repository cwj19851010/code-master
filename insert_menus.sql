-- 清空现有菜单数据
DELETE FROM sys_role_menus;
DELETE FROM sys_menus;

-- 插入系统管理目录
DECLARE @SystemMenuId BIGINT = 1885557337714819072;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@SystemMenuId, N'系统管理', NULL, 1, '/system', 'Layout', 'M', 0, 0, 'Setting', GETDATE(), NULL);

-- 插入用户管理菜单
DECLARE @UserMenuId BIGINT = 1885557337714819073;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@UserMenuId, N'用户管理', @SystemMenuId, 1, 'user', 'system/user/index', 'C', 0, 0, 'User', GETDATE(), 'system:user:list');

-- 插入角色管理菜单
DECLARE @RoleMenuId BIGINT = 1885557337714819074;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@RoleMenuId, N'角色管理', @SystemMenuId, 2, 'role', 'system/role/index', 'C', 0, 0, 'UserFilled', GETDATE(), 'system:role:list');

-- 插入部门管理菜单
DECLARE @DeptMenuId BIGINT = 1885557337714819075;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@DeptMenuId, N'部门管理', @SystemMenuId, 3, 'dept', 'system/dept/index', 'C', 0, 0, 'OfficeBuilding', GETDATE(), 'system:dept:list');

-- 插入菜单管理菜单
DECLARE @MenuMenuId BIGINT = 1885557337714819076;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@MenuMenuId, N'菜单管理', @SystemMenuId, 4, 'menu', 'system/menu/index', 'C', 0, 0, 'Menu', GETDATE(), 'system:menu:list');

-- 插入租户管理菜单
DECLARE @TenantMenuId BIGINT = 1885557337714819077;
INSERT INTO sys_menus (id, menu_name, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, perms)
VALUES (@TenantMenuId, N'租户管理', @SystemMenuId, 5, 'tenant', 'system/tenant/index', 'C', 0, 0, 'OfficeBuilding', GETDATE(), 'system:tenant:list');

-- 获取超级管理员角色ID（假设是第一个角色）
DECLARE @AdminRoleId BIGINT = (SELECT TOP 1 id FROM sys_roles WHERE role_key = 'admin');

-- 为超级管理员分配所有菜单权限
INSERT INTO sys_role_menus (role_id, menu_id)
VALUES
    (@AdminRoleId, @SystemMenuId),
    (@AdminRoleId, @UserMenuId),
    (@AdminRoleId, @RoleMenuId),
    (@AdminRoleId, @DeptMenuId),
    (@AdminRoleId, @MenuMenuId),
    (@AdminRoleId, @TenantMenuId);

PRINT '菜单数据插入完成！';
