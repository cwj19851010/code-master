-- 添加菜单管理相关翻译键
INSERT INTO sys_lang_texts (lang_id, lang_key, lang_value) VALUES
-- 中文翻译
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'menuName', '菜单名称'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'titleKey', '标题键'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'menuType', '菜单类型'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'directory', '目录'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'menu', '菜单'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'button', '按钮'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'orderNum', '排序号'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'icon', '图标'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'path', '路由路径'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'component', '组件路径'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'perms', '权限标识'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'visible', '显示状态'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'show', '显示'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'hide', '隐藏'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'isCache', '是否缓存'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'cache', '缓存'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'noCache', '不缓存'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'menuScope', '菜单范围'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'hostOnly', '宿主专用'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'tenantOnly', '租户专用'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'shared', '共享'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'zh-CN'), 'parentMenu', '上级菜单'),

-- 英文翻译
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'menuName', 'Menu Name'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'titleKey', 'Title Key'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'menuType', 'Menu Type'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'directory', 'Directory'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'menu', 'Menu'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'button', 'Button'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'orderNum', 'Order'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'icon', 'Icon'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'path', 'Path'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'component', 'Component'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'perms', 'Permissions'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'visible', 'Visible'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'show', 'Show'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'hide', 'Hide'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'isCache', 'Cache'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'cache', 'Cache'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'noCache', 'No Cache'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'menuScope', 'Menu Scope'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'hostOnly', 'Host Only'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'tenantOnly', 'Tenant Only'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'shared', 'Shared'),
((SELECT lang_id FROM sys_langs WHERE lang_code = 'en-US'), 'parentMenu', 'Parent Menu');

-- 添加菜单管理的隐藏路由（新增、编辑、详情）
INSERT INTO sys_menus (menu_id, menu_name, title_key, parent_id, order_num, path, component, menu_type, visible, status, perms, icon, create_time, is_cache) VALUES
(774733781762170, '菜单新增', 'addMenu', NULL, 1, '/system/menu/add', 'system/menu/add', 'C', 1, 0, 'system:menu:create', '', GETDATE(), 0),
(774733781762171, '菜单编辑', 'editMenu', NULL, 2, '/system/menu/edit', 'system/menu/edit', 'C', 1, 0, 'system:menu:update', '', GETDATE(), 0),
(774733781762172, '菜单详情', 'menuDetail', NULL, 3, '/system/menu/detail', 'system/menu/detail', 'C', 1, 0, 'system:menu:view', '', GETDATE(), 0);

-- 为admin角色分配新增的路由权限
INSERT INTO sys_role_menus (role_id, menu_id) VALUES
(1, 774733781762170),
(1, 774733781762171),
(1, 774733781762172);

-- 添加菜单管理的按钮权限
INSERT INTO sys_menus (menu_id, menu_name, title_key, parent_id, order_num, path, component, menu_type, visible, status, perms, icon, create_time) VALUES
(774733781762173, '菜单查看', 'viewMenu', 774733781725254, 1, '', '', 'F', 0, 0, 'system:menu:view', '', GETDATE()),
(774733781762174, '菜单新增', 'createMenu', 774733781725254, 2, '', '', 'F', 0, 0, 'system:menu:create', '', GETDATE()),
(774733781762175, '菜单修改', 'updateMenu', 774733781725254, 3, '', '', 'F', 0, 0, 'system:menu:update', '', GETDATE()),
(774733781762176, '菜单删除', 'deleteMenu', 774733781725254, 4, '', '', 'F', 0, 0, 'system:menu:delete', '', GETDATE());

-- 为admin角色分配按钮权限
INSERT INTO sys_role_menus (role_id, menu_id) VALUES
(1, 774733781762173),
(1, 774733781762174),
(1, 774733781762175),
(1, 774733781762176);
