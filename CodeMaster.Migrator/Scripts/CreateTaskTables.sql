-- 创建任务调度表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_tasks')
BEGIN
    CREATE TABLE sys_tasks (
        id BIGINT PRIMARY KEY,
        task_id BIGINT NOT NULL,
        task_name NVARCHAR(100) NOT NULL,
        job_group NVARCHAR(50) NOT NULL DEFAULT 'DEFAULT',
        task_type INT NOT NULL DEFAULT 1,
        invoke_target NVARCHAR(500),
        api_url NVARCHAR(500),
        request_method NVARCHAR(10),
        request_parameters NVARCHAR(MAX),
        sql_text NVARCHAR(MAX),
        cron_expression NVARCHAR(255),
        interval_second INT NOT NULL DEFAULT 60,
        run_times INT NOT NULL DEFAULT 0,
        begin_time DATETIME2,
        end_time DATETIME2,
        last_run_time DATETIME2,
        status INT NOT NULL DEFAULT 0,
        create_by NVARCHAR(50),
        create_user_id BIGINT,
        create_time DATETIME2 NOT NULL,
        update_by NVARCHAR(50),
        update_time DATETIME2,
        remark NVARCHAR(500),
        is_deleted BIT NOT NULL DEFAULT 0,
        delete_by NVARCHAR(50),
        delete_time DATETIME2
    )
    PRINT 'sys_tasks table created'
END

-- 创建任务日志表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_task_logs')
BEGIN
    CREATE TABLE sys_task_logs (
        id BIGINT PRIMARY KEY,
        task_id BIGINT NOT NULL,
        task_name NVARCHAR(100) NOT NULL,
        invoke_target NVARCHAR(500),
        status INT NOT NULL DEFAULT 0,
        elapsed FLOAT NOT NULL DEFAULT 0,
        job_message NVARCHAR(MAX),
        create_by NVARCHAR(50),
        create_user_id BIGINT,
        create_time DATETIME2 NOT NULL,
        update_by NVARCHAR(50),
        update_time DATETIME2,
        remark NVARCHAR(500),
        is_deleted BIT NOT NULL DEFAULT 0,
        delete_by NVARCHAR(50),
        delete_time DATETIME2
    )
    PRINT 'sys_task_logs table created'
END

PRINT 'Task scheduling tables setup completed'
GO
