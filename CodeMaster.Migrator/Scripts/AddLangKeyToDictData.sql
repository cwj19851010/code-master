-- 添加 lang_key 字段到 sys_dict_data 表
ALTER TABLE sys_dict_data ADD COLUMN lang_key VARCHAR(100) NULL COMMENT '国际化键';
