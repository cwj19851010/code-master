-- CodeMaster 数据库初始化脚本
-- 创建时间：2026-02-14

-- 创建数据库
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CodeMasterDb')
BEGIN
    CREATE DATABASE CodeMasterDb;
END
GO

USE CodeMasterDb;
GO

-- 系统用户表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_user')
BEGIN
    CREATE TABLE sys_user (
        id BIGINT PRIMARY KEY,
        user_name NVARCHAR(30) NOT NULL,
        nick_name NVARCHAR(30) NOT NULL,
        user_type NVARCHAR(2) DEFAULT '00',
        email NVARCHAR(50),
        phone_number NVARCHAR(11),
        sex INT DEFAULT 2,
        avatar NVARCHAR(500),
        password NVARCHAR(100) NOT NULL,
        status INT DEFAULT 0,
        del_flag INT DEFAULT 0,
        login_ip NVARCHAR(50),
        login_date DATETIME,
        dept_id BIGINT,
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 系统角色表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_role')
BEGIN
    CREATE TABLE sys_role (
        id BIGINT PRIMARY KEY,
        role_name NVARCHAR(30) NOT NULL,
        role_key NVARCHAR(100) NOT NULL,
        role_sort INT DEFAULT 0,
        data_scope INT DEFAULT 1,
        status INT DEFAULT 0,
        del_flag INT DEFAULT 0,
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 系统菜单表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_menu')
BEGIN
    CREATE TABLE sys_menu (
        id BIGINT PRIMARY KEY,
        menu_name NVARCHAR(50) NOT NULL,
        parent_id BIGINT DEFAULT 0,
        order_num INT DEFAULT 0,
        path NVARCHAR(200),
        component NVARCHAR(255),
        is_frame INT DEFAULT 0,
        is_cache INT DEFAULT 0,
        menu_type NVARCHAR(1) DEFAULT 'M',
        visible INT DEFAULT 0,
        status INT DEFAULT 0,
        perms NVARCHAR(100),
        icon NVARCHAR(100),
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 系统部门表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_dept')
BEGIN
    CREATE TABLE sys_dept (
        id BIGINT PRIMARY KEY,
        parent_id BIGINT DEFAULT 0,
        ancestors NVARCHAR(500),
        dept_name NVARCHAR(30) NOT NULL,
        order_num INT DEFAULT 0,
        leader NVARCHAR(20),
        phone NVARCHAR(11),
        email NVARCHAR(50),
        status INT DEFAULT 0,
        del_flag INT DEFAULT 0,
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 系统租户表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_tenant')
BEGIN
    CREATE TABLE sys_tenant (
        id BIGINT PRIMARY KEY,
        tenant_code NVARCHAR(50) NOT NULL UNIQUE,
        tenant_name NVARCHAR(100) NOT NULL,
        isolation_type INT DEFAULT 1,
        config_id NVARCHAR(50),
        connection_string NVARCHAR(500),
        db_type INT,
        status INT DEFAULT 0,
        expire_time DATETIME,
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 代码生成表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'gen_table')
BEGIN
    CREATE TABLE gen_table (
        id BIGINT PRIMARY KEY,
        table_name NVARCHAR(200) NOT NULL,
        entity_name NVARCHAR(100) NOT NULL,
        table_comment NVARCHAR(500),
        function_name NVARCHAR(100),
        module_id BIGINT NOT NULL,
        is_read_only INT DEFAULT 0,
        only_dto INT DEFAULT 0,
        is_tree INT DEFAULT 0,
        is_child INT DEFAULT 0,
        status INT DEFAULT 0,
        tree_parent_field NVARCHAR(50),
        tree_name_field NVARCHAR(50),
        function_author NVARCHAR(50),
        gen_path NVARCHAR(500),
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

-- 代码生成列表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'gen_table_column')
BEGIN
    CREATE TABLE gen_table_column (
        id BIGINT PRIMARY KEY,
        table_id BIGINT NOT NULL,
        column_name NVARCHAR(200) NOT NULL,
        property_name NVARCHAR(100) NOT NULL,
        column_comment NVARCHAR(500),
        column_type NVARCHAR(100),
        csharp_type NVARCHAR(50),
        is_pk INT DEFAULT 0,
        is_increment INT DEFAULT 0,
        is_required INT DEFAULT 0,
        show_in_list INT DEFAULT 1,
        show_in_add INT DEFAULT 1,
        show_in_edit INT DEFAULT 1,
        show_in_detail INT DEFAULT 1,
        is_query INT DEFAULT 0,
        query_type NVARCHAR(20) DEFAULT 'EQ',
        html_type NVARCHAR(50) DEFAULT 'input',
        dict_type NVARCHAR(200),
        sort INT DEFAULT 0,
        status INT DEFAULT 0,
        create_by NVARCHAR(64),
        create_time DATETIME DEFAULT GETDATE(),
        update_by NVARCHAR(64),
        update_time DATETIME,
        remark NVARCHAR(500)
    );
END
GO

PRINT 'CodeMaster 数据库初始化完成！';
