#!/bin/bash

# 批量替换 placeholder 中的翻译键为 t2 函数

cd "CodeMaster.Vue/src/views"

# 定义替换规则
declare -A replacements=(
    ["t('please_input_username')"]="t2('please_input', 'username')"
    ["t('please_input_password')"]="t2('please_input', 'password')"
    ["t('please_input_nickname')"]="t2('please_input', 'nickname')"
    ["t('please_input_email')"]="t2('please_input', 'email')"
    ["t('please_input_phone')"]="t2('please_input', 'phone')"
    ["t('please_input_name')"]="t2('please_input', 'name')"
    ["t('please_input_remark')"]="t2('please_input', 'remark')"
    ["t('please_input_project_name')"]="t2('please_input', 'project_name')"
    ["t('please_input_display_name')"]="t2('please_input', 'display_name')"
    ["t('please_input_connection_string')"]="t2('please_input', 'connection_string')"
    ["t('please_input_project_path')"]="t2('please_input', 'project_path')"
    ["t('please_select_dept')"]="t2('please_select', 'dept')"
    ["t('please_select_post')"]="t2('please_select', 'post')"
    ["t('please_select_role')"]="t2('please_select', 'role')"
    ["t('please_select_database_type')"]="t2('please_select', 'database_type')"
    ["t('please_input')"]="t2('please_input', '')"
    ["t('please_select')"]="t2('please_select', '')"
)

# 查找所有包含旧模式的文件
files=$(grep -r "placeholder.*t('please_" --include="*.vue" -l)

echo "找到 $(echo "$files" | wc -l) 个文件需要修改"

# 对每个文件进行替换
for file in $files; do
    echo "处理: $file"

    # 使用 sed 进行替换
    for old in "${!replacements[@]}"; do
        new="${replacements[$old]}"
        sed -i "s|$old|$new|g" "$file"
    done
done

echo "完成！"
