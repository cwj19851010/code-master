#!/bin/bash

# 为所有列表页面添加 defineOptions

# 定义需要处理的文件和对应的组件名称
declare -A files=(
    ["CodeMaster.Vue/src/views/system/user/index.vue"]="SystemUser"
    ["CodeMaster.Vue/src/views/system/dept/index.vue"]="SystemDept"
    ["CodeMaster.Vue/src/views/system/role/index.vue"]="SystemRole"
    ["CodeMaster.Vue/src/views/system/post/index.vue"]="SystemPost"
    ["CodeMaster.Vue/src/views/system/menu/index.vue"]="SystemMenu"
    ["CodeMaster.Vue/src/views/system/dict/index.vue"]="SystemDict"
    ["CodeMaster.Vue/src/views/system/tenant/index.vue"]="SystemTenant"
    ["CodeMaster.Vue/src/views/system/file/index.vue"]="SystemFile"
    ["CodeMaster.Vue/src/views/system/lang/index.vue"]="SystemLang"
    ["CodeMaster.Vue/src/views/monitor/loginlog/index.vue"]="MonitorLoginlog"
    ["CodeMaster.Vue/src/views/monitor/operlog/index.vue"]="MonitorOperlog"
    ["CodeMaster.Vue/src/views/monitor/task/index.vue"]="MonitorTask"
    ["CodeMaster.Vue/src/views/monitor/tasklog/index.vue"]="MonitorTasklog"
    ["CodeMaster.Vue/src/views/monitor/online/index.vue"]="MonitorOnline"
    ["CodeMaster.Vue/src/views/codegen/project/index.vue"]="CodegenProject"
    ["CodeMaster.Vue/src/views/codegen/projectModule/index.vue"]="CodegenProjectModule"
    ["CodeMaster.Vue/src/views/codegen/moduleEntity/index.vue"]="CodegenModuleEntity"
    ["CodeMaster.Vue/src/views/codegen/entityField/index.vue"]="CodegenEntityField"
)

for file in "${!files[@]}"; do
    component_name="${files[$file]}"
    full_path="$file"

    echo "Processing: $full_path -> $component_name"

    # 检查文件是否存在
    if [ ! -f "$full_path" ]; then
        echo "  File not found, skipping..."
        continue
    fi

    # 检查是否已经有 defineOptions
    if grep -q "defineOptions" "$full_path"; then
        echo "  Already has defineOptions, skipping..."
        continue
    fi

    # 在 <script setup> 后的 import 语句之后添加 defineOptions
    # 使用 awk 来处理
    awk -v name="$component_name" '
    BEGIN { added = 0 }
    {
        print $0
        if (!added && /^import.*from/) {
            # 找到最后一个 import 语句后添加空行和 defineOptions
            getline
            while (/^import.*from/) {
                print $0
                getline
            }
            print ""
            print "defineOptions({"
            print "  name: '\''" name "'\''"
            print "})"
            print ""
            print $0
            added = 1
            next
        }
    }
    ' "$full_path" > "$full_path.tmp" && mv "$full_path.tmp" "$full_path"

    echo "  Done!"
done

echo "All files processed!"
