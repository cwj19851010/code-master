-- 修复任务管理和任务日志的模块配置
-- 将 monitor 改为 system

-- 更新任务管理菜单
UPDATE sys_menus 
SET path = '/system/task', 
    component = 'system/task/index'
WHERE id = 774733781762147;

-- 更新任务日志菜单
UPDATE sys_menus 
SET path = '/system/tasklog', 
    component = 'system/tasklog/index'
WHERE id = 774733781762148;

-- 更新任务管理详情路由
UPDATE sys_menus 
SET path = '/system/task/detail', 
    component = 'system/task/detail'
WHERE menu_name = '任务详情' AND parent_id IS NULL;

-- 更新任务日志详情路由
UPDATE sys_menus 
SET path = '/system/tasklog/detail', 
    component = 'system/tasklog/detail'
WHERE id = 774733781762202;

SELECT 'Task menus updated successfully' as Result;
