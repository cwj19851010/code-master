-- 添加按钮级别的权限（menu_type='F'）
-- 为每个功能模块添加：查看、新增、编辑、删除、导出、导入等按钮权限

-- 用户管理按钮权限 (parent_id = 774733781762117)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762201, 774733781762117, 'system.user.view', 'F', 'system:user:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762202, 774733781762117, 'system.user.create', 'F', 'system:user:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762203, 774733781762117, 'system.user.update', 'F', 'system:user:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762204, 774733781762117, 'system.user.delete', 'F', 'system:user:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762205, 774733781762117, 'system.user.export', 'F', 'system:user:export', 5, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762206, 774733781762117, 'system.user.import', 'F', 'system:user:import', 6, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 角色管理按钮权限 (parent_id = 774733781762118)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762211, 774733781762118, 'system.role.view', 'F', 'system:role:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762212, 774733781762118, 'system.role.create', 'F', 'system:role:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762213, 774733781762118, 'system.role.update', 'F', 'system:role:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762214, 774733781762118, 'system.role.delete', 'F', 'system:role:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762215, 774733781762118, 'system.role.export', 'F', 'system:role:export', 5, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 部门管理按钮权限 (parent_id = 774733781762119)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762221, 774733781762119, 'system.dept.view', 'F', 'system:dept:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762222, 774733781762119, 'system.dept.create', 'F', 'system:dept:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762223, 774733781762119, 'system.dept.update', 'F', 'system:dept:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762224, 774733781762119, 'system.dept.delete', 'F', 'system:dept:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 菜单管理按钮权限 (parent_id = 774733781762120)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762231, 774733781762120, 'system.menu.view', 'F', 'system:menu:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762232, 774733781762120, 'system.menu.create', 'F', 'system:menu:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762233, 774733781762120, 'system.menu.update', 'F', 'system:menu:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762234, 774733781762120, 'system.menu.delete', 'F', 'system:menu:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 租户管理按钮权限 (parent_id = 774733781762121)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762241, 774733781762121, 'system.tenant.view', 'F', 'system:tenant:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762242, 774733781762121, 'system.tenant.create', 'F', 'system:tenant:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762243, 774733781762121, 'system.tenant.update', 'F', 'system:tenant:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762244, 774733781762121, 'system.tenant.delete', 'F', 'system:tenant:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 岗位管理按钮权限 (parent_id = 774733781762122)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762251, 774733781762122, 'system.post.view', 'F', 'system:post:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762252, 774733781762122, 'system.post.create', 'F', 'system:post:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762253, 774733781762122, 'system.post.update', 'F', 'system:post:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762254, 774733781762122, 'system.post.delete', 'F', 'system:post:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762255, 774733781762122, 'system.post.export', 'F', 'system:post:export', 5, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 字典管理按钮权限 (parent_id = 774733781762123)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762261, 774733781762123, 'system.dict.view', 'F', 'system:dict:type:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762262, 774733781762123, 'system.dict.create', 'F', 'system:dict:type:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762263, 774733781762123, 'system.dict.update', 'F', 'system:dict:type:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762264, 774733781762123, 'system.dict.delete', 'F', 'system:dict:type:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762265, 774733781762123, 'system.dict.export', 'F', 'system:dict:type:export', 5, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 语言管理按钮权限 (parent_id = 774733781762124)
INSERT INTO sys_menus (id, parent_id, menu_name, menu_type, perms, order_num, visible, status, is_frame, is_cache, create_time, update_time, is_deleted)
VALUES
(774733781762271, 774733781762124, 'system.lang.view', 'F', 'system:lang:view', 1, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762272, 774733781762124, 'system.lang.create', 'F', 'system:lang:create', 2, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762273, 774733781762124, 'system.lang.update', 'F', 'system:lang:update', 3, '0', '0', 1, 0, GETDATE(), GETDATE(), 0),
(774733781762274, 774733781762124, 'system.lang.delete', 'F', 'system:lang:delete', 4, '0', '0', 1, 0, GETDATE(), GETDATE(), 0);

-- 为admin角色分配所有按钮权限
-- 首先获取admin角色的ID
DECLARE @adminRoleId BIGINT
SELECT @adminRoleId = id FROM sys_roles WHERE role_key = 'admin'

-- 插入角色菜单关系
INSERT INTO sys_role_menus (role_id, menu_id)
SELECT @adminRoleId, id FROM sys_menus WHERE menu_type = 'F' AND id >= 774733781762201 AND id <= 774733781762274
AND NOT EXISTS (SELECT 1 FROM sys_role_menus WHERE role_id = @adminRoleId AND menu_id = sys_menus.id);

-- 添加按钮权限的翻译
INSERT INTO sys_lang_texts (id, lang_code, lang_key, lang_value, category, create_time, is_deleted)
VALUES
-- 用户管理按钮
(774733782380701, 'zh-CN', 'system.user.view', '查看', 'button', GETDATE(), 0),
(774733782380702, 'en-US', 'system.user.view', 'View', 'button', GETDATE(), 0),
(774733782380703, 'zh-CN', 'system.user.create', '新增', 'button', GETDATE(), 0),
(774733782380704, 'en-US', 'system.user.create', 'Create', 'button', GETDATE(), 0),
(774733782380705, 'zh-CN', 'system.user.update', '编辑', 'button', GETDATE(), 0),
(774733782380706, 'en-US', 'system.user.update', 'Update', 'button', GETDATE(), 0),
(774733782380707, 'zh-CN', 'system.user.delete', '删除', 'button', GETDATE(), 0),
(774733782380708, 'en-US', 'system.user.delete', 'Delete', 'button', GETDATE(), 0),
(774733782380709, 'zh-CN', 'system.user.export', '导出', 'button', GETDATE(), 0),
(774733782380710, 'en-US', 'system.user.export', 'Export', 'button', GETDATE(), 0),
(774733782380711, 'zh-CN', 'system.user.import', '导入', 'button', GETDATE(), 0),
(774733782380712, 'en-US', 'system.user.import', 'Import', 'button', GETDATE(), 0),

-- 角色管理按钮
(774733782380713, 'zh-CN', 'system.role.view', '查看', 'button', GETDATE(), 0),
(774733782380714, 'en-US', 'system.role.view', 'View', 'button', GETDATE(), 0),
(774733782380715, 'zh-CN', 'system.role.create', '新增', 'button', GETDATE(), 0),
(774733782380716, 'en-US', 'system.role.create', 'Create', 'button', GETDATE(), 0),
(774733782380717, 'zh-CN', 'system.role.update', '编辑', 'button', GETDATE(), 0),
(774733782380718, 'en-US', 'system.role.update', 'Update', 'button', GETDATE(), 0),
(774733782380719, 'zh-CN', 'system.role.delete', '删除', 'button', GETDATE(), 0),
(774733782380720, 'en-US', 'system.role.delete', 'Delete', 'button', GETDATE(), 0),
(774733782380721, 'zh-CN', 'system.role.export', '导出', 'button', GETDATE(), 0),
(774733782380722, 'en-US', 'system.role.export', 'Export', 'button', GETDATE(), 0),

-- 部门管理按钮
(774733782380723, 'zh-CN', 'system.dept.view', '查看', 'button', GETDATE(), 0),
(774733782380724, 'en-US', 'system.dept.view', 'View', 'button', GETDATE(), 0),
(774733782380725, 'zh-CN', 'system.dept.create', '新增', 'button', GETDATE(), 0),
(774733782380726, 'en-US', 'system.dept.create', 'Create', 'button', GETDATE(), 0),
(774733782380727, 'zh-CN', 'system.dept.update', '编辑', 'button', GETDATE(), 0),
(774733782380728, 'en-US', 'system.dept.update', 'Update', 'button', GETDATE(), 0),
(774733782380729, 'zh-CN', 'system.dept.delete', '删除', 'button', GETDATE(), 0),
(774733782380730, 'en-US', 'system.dept.delete', 'Delete', 'button', GETDATE(), 0),

-- 菜单管理按钮
(774733782380731, 'zh-CN', 'system.menu.view', '查看', 'button', GETDATE(), 0),
(774733782380732, 'en-US', 'system.menu.view', 'View', 'button', GETDATE(), 0),
(774733782380733, 'zh-CN', 'system.menu.create', '新增', 'button', GETDATE(), 0),
(774733782380734, 'en-US', 'system.menu.create', 'Create', 'button', GETDATE(), 0),
(774733782380735, 'zh-CN', 'system.menu.update', '编辑', 'button', GETDATE(), 0),
(774733782380736, 'en-US', 'system.menu.update', 'Update', 'button', GETDATE(), 0),
(774733782380737, 'zh-CN', 'system.menu.delete', '删除', 'button', GETDATE(), 0),
(774733782380738, 'en-US', 'system.menu.delete', 'Delete', 'button', GETDATE(), 0),

-- 租户管理按钮
(774733782380739, 'zh-CN', 'system.tenant.view', '查看', 'button', GETDATE(), 0),
(774733782380740, 'en-US', 'system.tenant.view', 'View', 'button', GETDATE(), 0),
(774733782380741, 'zh-CN', 'system.tenant.create', '新增', 'button', GETDATE(), 0),
(774733782380742, 'en-US', 'system.tenant.create', 'Create', 'button', GETDATE(), 0),
(774733782380743, 'zh-CN', 'system.tenant.update', '编辑', 'button', GETDATE(), 0),
(774733782380744, 'en-US', 'system.tenant.update', 'Update', 'button', GETDATE(), 0),
(774733782380745, 'zh-CN', 'system.tenant.delete', '删除', 'button', GETDATE(), 0),
(774733782380746, 'en-US', 'system.tenant.delete', 'Delete', 'button', GETDATE(), 0),

-- 岗位管理按钮
(774733782380747, 'zh-CN', 'system.post.view', '查看', 'button', GETDATE(), 0),
(774733782380748, 'en-US', 'system.post.view', 'View', 'button', GETDATE(), 0),
(774733782380749, 'zh-CN', 'system.post.create', '新增', 'button', GETDATE(), 0),
(774733782380750, 'en-US', 'system.post.create', 'Create', 'button', GETDATE(), 0),
(774733782380751, 'zh-CN', 'system.post.update', '编辑', 'button', GETDATE(), 0),
(774733782380752, 'en-US', 'system.post.update', 'Update', 'button', GETDATE(), 0),
(774733782380753, 'zh-CN', 'system.post.delete', '删除', 'button', GETDATE(), 0),
(774733782380754, 'en-US', 'system.post.delete', 'Delete', 'button', GETDATE(), 0),
(774733782380755, 'zh-CN', 'system.post.export', '导出', 'button', GETDATE(), 0),
(774733782380756, 'en-US', 'system.post.export', 'Export', 'button', GETDATE(), 0),

-- 字典管理按钮
(774733782380757, 'zh-CN', 'system.dict.view', '查看', 'button', GETDATE(), 0),
(774733782380758, 'en-US', 'system.dict.view', 'View', 'button', GETDATE(), 0),
(774733782380759, 'zh-CN', 'system.dict.create', '新增', 'button', GETDATE(), 0),
(774733782380760, 'en-US', 'system.dict.create', 'Create', 'button', GETDATE(), 0),
(774733782380761, 'zh-CN', 'system.dict.update', '编辑', 'button', GETDATE(), 0),
(774733782380762, 'en-US', 'system.dict.update', 'Update', 'button', GETDATE(), 0),
(774733782380763, 'zh-CN', 'system.dict.delete', '删除', 'button', GETDATE(), 0),
(774733782380764, 'en-US', 'system.dict.delete', 'Delete', 'button', GETDATE(), 0),
(774733782380765, 'zh-CN', 'system.dict.export', '导出', 'button', GETDATE(), 0),
(774733782380766, 'en-US', 'system.dict.export', 'Export', 'button', GETDATE(), 0),

-- 语言管理按钮
(774733782380767, 'zh-CN', 'system.lang.view', '查看', 'button', GETDATE(), 0),
(774733782380768, 'en-US', 'system.lang.view', 'View', 'button', GETDATE(), 0),
(774733782380769, 'zh-CN', 'system.lang.create', '新增', 'button', GETDATE(), 0),
(774733782380770, 'en-US', 'system.lang.create', 'Create', 'button', GETDATE(), 0),
(774733782380771, 'zh-CN', 'system.lang.update', '编辑', 'button', GETDATE(), 0),
(774733782380772, 'en-US', 'system.lang.update', 'Update', 'button', GETDATE(), 0),
(774733782380773, 'zh-CN', 'system.lang.delete', '删除', 'button', GETDATE(), 0),
(774733782380774, 'en-US', 'system.lang.delete', 'Delete', 'button', GETDATE(), 0);

PRINT '按钮权限添加完成！'
