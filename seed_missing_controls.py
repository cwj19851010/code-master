"""Seed missing control templates into the database"""
import sqlite3, json

db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')
now = '2026-06-07 00:00:00.0000000'
empty = json.dumps({"imports":[],"uses":[],"refs":[],"reactives":[],"functions":[],"hooks":[],"computed":[],"watches":[]})

# Script section JSONs (matching TemplateModule.cs C# output)
editor_script = json.dumps({
    "imports": [
        {"path": "@wangeditor/editor", "destructured": "createEditor, createToolbar"},
        {"path": "@wangeditor/editor/dist/css/style.css"}
    ],
    "uses": [], "refs": [
        {"name": "[field.nameLower]EditorInstance", "initialValue": "null"}
    ],
    "reactives": [],
    "functions": [{
        "name": "init[field.name]Editor",
        "async": True,
        "body": [
            "const { createEditor, createToolbar } = await import('@wangeditor/editor');",
            "await import('@wangeditor/editor/dist/css/style.css');",
            "const editor = createEditor({",
            "  selector: '#[field.nameLower]Editor',",
            "  html: form.[field.nameLower] || '',",
            "  config: { placeholder: $t('[field.description]') }",
            "});",
            "createToolbar({ editor, selector: '#[field.nameLower]Toolbar', config: {} });",
            "[field.nameLower]EditorInstance.value = editor;"
        ]
    }],
    "hooks": [
        {"name": "onMounted", "body": ["init[field.name]Editor();"]},
        {"name": "onUnmounted", "body": ["if ([field.nameLower]EditorInstance.value) { [field.nameLower]EditorInstance.value.destroy(); }"]}
    ],
    "computed": [], "watches": []
})

dict_script = json.dumps({
    "imports": [{"path": "@/api/system/dict", "destructured": "getDataListByType"}],
    "uses": [], "refs": [{"name": "[field.nameLower]Options", "initialValue": "[]"}],
    "reactives": [], "functions": [], "hooks": [], "computed": [], "watches": []
})

select_table_script = json.dumps({
    "imports": [{"path": "@/api/[gen.moduleNameLower]/[field.relatedEntityNameLower]", "destructured": "getList as get[field.relatedEntityName]List"}],
    "uses": [], "refs": [{"name": "[field.relatedEntityNameLower]Options", "initialValue": "[]"}],
    "reactives": [], "functions": [], "hooks": [], "computed": [], "watches": []
})

file_script = json.dumps({
    "imports": [{"path": "element-plus", "destructured": "ElMessage"}],
    "uses": [], "refs": [], "reactives": [],
    "functions": [{"name": "beforeImageUpload", "body": [
        "const isImage = file.type.startsWith('image/');",
        "if (!isImage) { ElMessage.error($t('onlyImageAllowed')); return false; }",
        "const isLt2M = file.size / 1024 / 1024 < 2;",
        "if (!isLt2M) { ElMessage.error($t('fileSizeLimit')); return false; }",
        "return true;"
    ]}],
    "hooks": [], "computed": [], "watches": []
})

image_script = json.dumps({
    "imports": [
        {"path": "element-plus", "destructured": "ElMessage"},
        {"path": "@element-plus/icons-vue", "destructured": "Plus"}
    ],
    "uses": [], "refs": [], "reactives": [],
    "functions": [{"name": "beforeImageUpload", "body": [
        "const isImage = file.type.startsWith('image/');",
        "if (!isImage) { ElMessage.error($t('onlyImageAllowed')); return false; }",
        "const isLt2M = file.size / 1024 / 1024 < 2;",
        "if (!isLt2M) { ElMessage.error($t('fileSizeLimit')); return false; }",
        "return true;"
    ]}],
    "hooks": [], "computed": [], "watches": []
})

controls = [
    # select-table
    ("select-table", "add",  '<el-form-item :label="$t(\'[field.relatedDisplayLabel]\')" prop="[field.nameLower][field.multipleSuffix]" data-gen-id="gen_field_[field.name]"><el-select v-model="form.[field.nameLower]" style="width:100%" clearable :placeholder="$t(\'[field.relatedDisplayLabel]\')"><el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>', select_table_script, 21),
    ("select-table", "edit", '<el-form-item :label="$t(\'[field.relatedDisplayLabel]\')" prop="[field.nameLower][field.multipleSuffix]" data-gen-id="gen_field_[field.name]"><el-select v-model="form.[field.nameLower]" style="width:100%" clearable :placeholder="$t(\'[field.relatedDisplayLabel]\')"><el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>', select_table_script, 22),
    # editor
    ("editor", "add",  '<div class="editor-form-item" data-gen-id="gen_field_[field.name]"><div class="editor-wrap"><div :id="\'[field.nameLower]Toolbar\'" class="editor-toolbar"></div><div :id="\'[field.nameLower]Editor\'" class="editor-content"></div></div></div>', editor_script, 23),
    ("editor", "edit", '<div class="editor-form-item" data-gen-id="gen_field_[field.name]"><div class="editor-wrap"><div :id="\'[field.nameLower]Toolbar\'" class="editor-toolbar"></div><div :id="\'[field.nameLower]Editor\'" class="editor-content"></div></div></div>', editor_script, 24),
    # file
    ("file", "add",  '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-upload :before-upload="beforeImageUpload" :on-success="(res) => form.[field.nameLower] = res.data" :file-list="form.[field.nameLower] ? [{ url: form.[field.nameLower] }] : []" list-type="picture" :limit="1"><el-button size="small" type="primary">{{ $t(\'upload\') }}</el-button></el-upload></el-form-item>', file_script, 25),
    ("file", "edit", '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-upload :before-upload="beforeImageUpload" :on-success="(res) => form.[field.nameLower] = res.data" :file-list="form.[field.nameLower] ? [{ url: form.[field.nameLower] }] : []" list-type="picture" :limit="1"><el-button size="small" type="primary">{{ $t(\'upload\') }}</el-button></el-upload></el-form-item>', file_script, 26),
    # image
    ("image", "add",  '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-upload :before-upload="beforeImageUpload" :on-success="(res) => form.[field.nameLower] = res.data" :file-list="form.[field.nameLower] ? [{ url: form.[field.nameLower] }] : []" list-type="picture-card" :limit="1"><el-icon><Plus /></el-icon></el-upload></el-form-item>', image_script, 27),
    ("image", "edit", '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-upload :before-upload="beforeImageUpload" :on-success="(res) => form.[field.nameLower] = res.data" :file-list="form.[field.nameLower] ? [{ url: form.[field.nameLower] }] : []" list-type="picture-card" :limit="1"><el-icon><Plus /></el-icon></el-upload></el-form-item>', image_script, 28),
    # cascader
    ("cascader", "add",  '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-cascader v-model="form.[field.nameLower]" :options="[field.relatedEntityNameLower]Tree" :props="{ value: \'id\', label: \'[field.relatedDisplayLabel]\', checkStrictly: true, emitPath: false }" style="width:100%" clearable /></el-form-item>', empty, 29),
    ("cascader", "edit", '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-cascader v-model="form.[field.nameLower]" :options="[field.relatedEntityNameLower]Tree" :props="{ value: \'id\', label: \'[field.relatedDisplayLabel]\', checkStrictly: true, emitPath: false }" style="width:100%" clearable /></el-form-item>', empty, 30),
]

for ct, ps, html, sc, sort in controls:
    exists = db.execute('SELECT id FROM sys_field_control_templates WHERE control_type=? AND page_section=? AND is_deleted=0', (ct, ps)).fetchone()
    if not exists:
        db.execute('INSERT INTO sys_field_control_templates (control_type, page_section, html_content, script_sections, sort, create_time, is_deleted) VALUES (?,?,?,?,?,?,0)',
                   (ct, ps, html, sc, sort, now))
        print(f'  + {ct}/{ps}')

db.commit()
db.close()
print('Done seeding missing controls')
