-- 部门模块菜单
-- 假设部门列表菜单ID为 776655936335873（需要根据实际情况调整）
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, path, component, menu_type, perms, icon, order_num, visible, status, is_cache, menu_scope, create_time, update_time)
VALUES 
-- 部门新增页面
(776655936335874, 776655936335873, '部门新增', 'dept_add', 'add', 'system/dept/add', 'C', 'system:dept:create', '', 1, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 部门编辑页面
(776655936335875, 776655936335873, '部门编辑', 'dept_edit', 'edit', 'system/dept/edit', 'C', 'system:dept:update', '', 2, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 部门详情页面
(776655936335876, 776655936335873, '部门详情', 'dept_detail', 'detail', 'system/dept/detail', 'C', 'system:dept:query', '', 3, 0, 0, 0, 2, datetime('now'), datetime('now'));

-- 字典类型模块菜单
-- 假设字典类型列表菜单ID为 776655936335877（需要根据实际情况调整）
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, path, component, menu_type, perms, icon, order_num, visible, status, is_cache, menu_scope, create_time, update_time)
VALUES 
-- 字典类型新增页面
(776655936335878, 776655936335877, '字典类型新增', 'dict_type_add', 'type/add', 'system/dict/type/add', 'C', 'system:dict:create', '', 1, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 字典类型编辑页面
(776655936335879, 776655936335877, '字典类型编辑', 'dict_type_edit', 'type/edit', 'system/dict/type/edit', 'C', 'system:dict:update', '', 2, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 字典类型详情页面
(776655936335880, 776655936335877, '字典类型详情', 'dict_type_detail', 'type/detail', 'system/dict/type/detail', 'C', 'system:dict:query', '', 3, 0, 0, 0, 2, datetime('now'), datetime('now'));

-- 字典数据模块菜单
-- 假设字典数据列表菜单ID为 776655936335881（需要根据实际情况调整）
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, path, component, menu_type, perms, icon, order_num, visible, status, is_cache, menu_scope, create_time, update_time)
VALUES 
-- 字典数据新增页面
(776655936335882, 776655936335881, '字典数据新增', 'dict_data_add', 'data/add', 'system/dict/data/add', 'C', 'system:dict:create', '', 1, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 字典数据编辑页面
(776655936335883, 776655936335881, '字典数据编辑', 'dict_data_edit', 'data/edit', 'system/dict/data/edit', 'C', 'system:dict:update', '', 2, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 字典数据详情页面
(776655936335884, 776655936335881, '字典数据详情', 'dict_data_detail', 'data/detail', 'system/dict/data/detail', 'C', 'system:dict:query', '', 3, 0, 0, 0, 2, datetime('now'), datetime('now'));

-- 语言模块菜单
-- 假设语言列表菜单ID为 776655936335885（需要根据实际情况调整）
INSERT INTO sys_menus (id, parent_id, menu_name, title_key, path, component, menu_type, perms, icon, order_num, visible, status, is_cache, menu_scope, create_time, update_time)
VALUES 
-- 语言新增页面
(776655936335886, 776655936335885, '语言新增', 'lang_add', 'add', 'system/lang/add', 'C', 'system:lang:create', '', 1, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 语言编辑页面
(776655936335887, 776655936335885, '语言编辑', 'lang_edit', 'edit', 'system/lang/edit', 'C', 'system:lang:update', '', 2, 0, 0, 0, 2, datetime('now'), datetime('now')),
-- 语言详情页面
(776655936335888, 776655936335885, '语言详情', 'lang_detail', 'detail', 'system/lang/detail', 'C', 'system:lang:query', '', 3, 0, 0, 0, 2, datetime('now'), datetime('now'));
