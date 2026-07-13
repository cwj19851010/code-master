-- 添加字典相关翻译
INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'zh-CN', 'lang_key', '国际化键'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'zh-CN' AND lang_key = 'lang_key');

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'en-US', 'lang_key', 'Lang Key'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'en-US' AND lang_key = 'lang_key');

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'zh-CN', 'please_input_lang_key', '请输入国际化键'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'zh-CN' AND lang_key = 'please_input_lang_key');

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'en-US', 'please_input_lang_key', 'Please input lang key'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'en-US' AND lang_key = 'please_input_lang_key');

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'zh-CN', 'lang_key_tip', '如果填写了国际化键，前端将优先显示翻译文本；如果为空，则直接显示标签内容'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'zh-CN' AND lang_key = 'lang_key_tip');

INSERT INTO sys_lang_texts (lang_code, lang_key, lang_value)
SELECT 'en-US', 'lang_key_tip', 'If lang key is provided, the frontend will display translated text; otherwise, it will display the label directly'
WHERE NOT EXISTS (SELECT 1 FROM sys_lang_texts WHERE lang_code = 'en-US' AND lang_key = 'lang_key_tip');
