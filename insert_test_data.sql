-- 插入测试数据

-- 1. 插入部门数据
IF NOT EXISTS (SELECT 1 FROM sys_depts WHERE id = 100)
BEGIN
    INSERT INTO sys_depts (id, parent_id, dept_name, order_num, leader, phone, email, status, create_time, update_time)
    VALUES
    (100, 0, '总公司', 0, '张三', '15888888888', 'admin@example.com', 0, GETDATE(), GETDATE()),
    (101, 100, '研发部', 1, '李四', '15888888889', 'dev@example.com', 0, GETDATE(), GETDATE()),
    (102, 100, '市场部', 2, '王五', '15888888890', 'market@example.com', 0, GETDATE(), GETDATE()),
    (103, 100, '财务部', 3, '赵六', '15888888891', 'finance@example.com', 0, GETDATE(), GETDATE()),
    (104, 101, '前端组', 1, '钱七', '15888888892', 'frontend@example.com', 0, GETDATE(), GETDATE()),
    (105, 101, '后端组', 2, '孙八', '15888888893', 'backend@example.com', 0, GETDATE(), GETDATE());
END

-- 2. 插入角色数据（如果不存在）
IF NOT EXISTS (SELECT 1 FROM sys_roles WHERE id = 2)
BEGIN
    INSERT INTO sys_roles (id, role_name, role_key, role_sort, data_scope, status, remark, create_time, update_time)
    VALUES
    (2, '普通用户', 'common', 2, 5, 0, '普通用户角色', GETDATE(), GETDATE()),
    (3, '部门经理', 'manager', 3, 3, 0, '部门经理角色', GETDATE(), GETDATE()),
    (4, '财务人员', 'finance', 4, 3, 0, '财务人员角色', GETDATE(), GETDATE());
END

-- 3. 更新 admin 用户的部门
UPDATE sys_users SET dept_id = 100 WHERE user_name = 'admin';

-- 4. 插入测试用户（密码都是 admin123 的 BCrypt 哈希）
IF NOT EXISTS (SELECT 1 FROM sys_users WHERE user_name = 'zhangsan')
BEGIN
    INSERT INTO sys_users (id, user_name, nick_name, password, email, phone, sex, avatar, status, dept_id, create_time, update_time, create_user_id)
    VALUES
    (2, 'zhangsan', '张三', '$2a$11$rOzjGHQGW8zAove3FKPqHOJ8RZXmNxnTvFQqLXxJKLLqKkZ5Qs5Gy', 'zhangsan@example.com', '15888888801', 1, NULL, 0, 101, GETDATE(), GETDATE(), 1),
    (3, 'lisi', '李四', '$2a$11$rOzjGHQGW8zAove3FKPqHOJ8RZXmNxnTvFQqLXxJKLLqKkZ5Qs5Gy', 'lisi@example.com', '15888888802', 1, NULL, 0, 101, GETDATE(), GETDATE(), 1),
    (4, 'wangwu', '王五', '$2a$11$rOzjGHQGW8zAove3FKPqHOJ8RZXmNxnTvFQqLXxJKLLqKkZ5Qs5Gy', 'wangwu@example.com', '15888888803', 1, NULL, 0, 102, GETDATE(), GETDATE(), 1),
    (5, 'zhaoliu', '赵六', '$2a$11$rOzjGHQGW8zAove3FKPqHOJ8RZXmNxnTvFQqLXxJKLLqKkZ5Qs5Gy', 'zhaoliu@example.com', '15888888804', 2, NULL, 0, 103, GETDATE(), GETDATE(), 1),
    (6, 'qianqi', '钱七', '$2a$11$rOzjGHQGW8zAove3FKPqHOJ8RZXmNxnTvFQqLXxJKLLqKkZ5Qs5Gy', 'qianqi@example.com', '15888888805', 1, NULL, 0, 104, GETDATE(), GETDATE(), 1);
END

-- 5. 插入用户角色关联
IF NOT EXISTS (SELECT 1 FROM sys_user_roles WHERE user_id = 2)
BEGIN
    INSERT INTO sys_user_roles (user_id, role_id)
    VALUES
    (2, 2), -- 张三 - 普通用户
    (3, 2), -- 李四 - 普通用户
    (4, 3), -- 王五 - 部门经理
    (5, 4), -- 赵六 - 财务人员
    (6, 2); -- 钱七 - 普通用户
END

-- 6. 为新角色分配菜单权限（分配系统管理下的所有菜单）
IF NOT EXISTS (SELECT 1 FROM sys_role_menus WHERE role_id = 2)
BEGIN
    -- 普通用户：只能查看用户管理
    INSERT INTO sys_role_menus (role_id, menu_id)
    SELECT 2, id FROM sys_menus WHERE id IN (1, 2);

    -- 部门经理：可以管理用户和部门
    INSERT INTO sys_role_menus (role_id, menu_id)
    SELECT 3, id FROM sys_menus WHERE id IN (1, 2, 4);

    -- 财务人员：可以查看所有菜单
    INSERT INTO sys_role_menus (role_id, menu_id)
    SELECT 4, id FROM sys_menus;
END

PRINT '测试数据插入完成！';
PRINT '用户账号：';
PRINT '  admin / admin123 (超级管理员)';
PRINT '  zhangsan / admin123 (普通用户 - 研发部)';
PRINT '  lisi / admin123 (普通用户 - 研发部)';
PRINT '  wangwu / admin123 (部门经理 - 市场部)';
PRINT '  zhaoliu / admin123 (财务人员 - 财务部)';
PRINT '  qianqi / admin123 (普通用户 - 前端组)';
