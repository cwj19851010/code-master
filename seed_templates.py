import sqlite3, json

db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')
now = '2026-06-06 00:00:00.0000000'

# Child card template (add/edit)
card_html = """<el-card shadow="never" style="margin-top:20px" data-gen-id="gen_child_[relation.entityName]"><template #header><div class="card-header"><span>{{ $t('[relation.entityNameAllLower]') }}</span><el-button type="primary" size="small" @click="handleAdd[relation.entityName]">{{ $t('add') }}</el-button></div></template><el-table :data="form.[relation.entityNameLower]s.filter(i => i.rowStatus !== 3)" border stripe>[relation.tableColumns]<el-table-column :label="$t('operation')" width="140"><template #default="scope"><el-button link type="primary" size="small" @click="handleEdit[relation.entityName](scope.row, scope.$index)">{{ $t('edit') }}</el-button><el-button link type="danger" size="small" @click="handleRemove[relation.entityName](scope.$index)">{{ $t('delete') }}</el-button></template></el-table-column></el-table></el-card>"""

card_script = {
    'functions': [
        {'name': 'handleAdd[relation.entityName]', 'params': '', 'body': [
            '[relation.entityNameLower]EditingIndex = -1;',
            'Object.keys([relation.entityNameLower]Form).forEach(k => {',
            '  if (typeof [relation.entityNameLower]Form[k] === "string") [relation.entityNameLower]Form[k] = "";',
            '  else if (typeof [relation.entityNameLower]Form[k] === "number") [relation.entityNameLower]Form[k] = 0;',
            '  else [relation.entityNameLower]Form[k] = null;',
            '});',
            '[relation.entityNameLower]Form.rowStatus = 0;',
            '[relation.entityNameLower]DialogVisible = true;'
        ]},
        {'name': 'handleEdit[relation.entityName]', 'params': 'row, index', 'body': [
            '[relation.entityNameLower]EditingIndex = index;',
            'Object.assign([relation.entityNameLower]Form, row);',
            '[relation.entityNameLower]DialogVisible = true;'
        ]},
        {'name': 'handleRemove[relation.entityName]', 'params': 'index', 'body': [
            'const item = form.[relation.entityNameLower]s[index];',
            'if (!item.id) { form.[relation.entityNameLower]s.splice(index, 1); }',
            'else { item.rowStatus = 3; }'
        ]},
        {'name': 'handle[relation.entityName]Submit', 'params': '', 'body': [
            'if (![relation.entityNameLower]FormRef.value) return;',
            'await [relation.entityNameLower]FormRef.value.validate(async (valid) => {',
            '  if (valid) {',
            '    if ([relation.entityNameLower]EditingIndex >= 0) {',
            '      Object.assign(form.[relation.entityNameLower]s[[relation.entityNameLower]EditingIndex], [relation.entityNameLower]Form);',
            '    } else {',
            '      form.[relation.entityNameLower]s.push({ ... [relation.entityNameLower]Form });',
            '    }',
            '    [relation.entityNameLower]DialogVisible = false;',
            '  }',
            '});'
        ], 'async': True}
    ],
    'refs': [
        {'name': '[relation.entityNameLower]FormRef'},
        {'name': '[relation.entityNameLower]EditingIndex', 'initialValue': '-1'},
        {'name': '[relation.entityNameLower]DialogVisible', 'initialValue': 'false'}
    ],
    'reactives': [
        {'name': '[relation.entityNameLower]Form', 'fields': {}}
    ]
}
for pt in ['add', 'edit']:
    c = db.execute('SELECT id FROM sys_child_templates WHERE page_type=? AND child_type=?', (pt, 'card')).fetchone()
    if not c:
        db.execute('INSERT INTO sys_child_templates (page_type, child_type, html_content, script_sections, sort, create_time, is_deleted) VALUES (?,?,?,?,?,?,0)',
            (pt, 'card', card_html, json.dumps(card_script, ensure_ascii=False), 1, now))
print('Card templates inserted')

# Child dialog template
dialog_html = """<el-dialog v-model="[relation.entityNameLower]DialogVisible" :title="$t('[relation.entityNameAllLower]') + ([relation.entityNameLower]EditingIndex >= 0 ? $t('edit') : $t('add'))" width="700px"><el-form ref="[relation.entityNameLower]FormRef" :model="[relation.entityNameLower]Form" label-width="100px"><el-row :gutter="16">[relation.dialogColumns]</el-row></el-form><template #footer><el-button @click="[relation.entityNameLower]DialogVisible = false">{{ $t('cancel') }}</el-button><el-button type="primary" @click="handle[relation.entityName]Submit">{{ $t('confirm') }}</el-button></template></el-dialog>"""
dialog_script = {'imports':[],'uses':[],'refs':[],'reactives':[],'functions':[],'hooks':[],'computed':[],'watches':[]}
for pt in ['add', 'edit']:
    c = db.execute('SELECT id FROM sys_child_templates WHERE page_type=? AND child_type=?', (pt, 'dialog')).fetchone()
    if not c:
        db.execute('INSERT INTO sys_child_templates (page_type, child_type, html_content, script_sections, sort, create_time, is_deleted) VALUES (?,?,?,?,?,?,0)',
            (pt, 'dialog', dialog_html, json.dumps(dialog_script, ensure_ascii=False), 2, now))
print('Dialog templates inserted')

# === CONTROL TEMPLATES ===
empty_script = {'imports':[],'uses':[],'refs':[],'reactives':[],'functions':[],'hooks':[],'computed':[],'watches':[]}

controls = [
    # (control_type, page_section, html, script_json)
    # add/edit form controls
    ('input', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input v-model="form.[field.nameLower]" :placeholder="$t(\'[field.description]\')" clearable [field.isRequired] /></el-form-item>', empty_script),
    ('input', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input v-model="form.[field.nameLower]" :placeholder="$t(\'[field.description]\')" clearable /></el-form-item>', empty_script),
    ('number', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input-number v-model="form.[field.nameLower]" :placeholder="$t(\'[field.description]\')" style="width:100%" /></el-form-item>', empty_script),
    ('number', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input-number v-model="form.[field.nameLower]" :placeholder="$t(\'[field.description]\')" style="width:100%" /></el-form-item>', empty_script),
    ('textarea', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input v-model="form.[field.nameLower]" type="textarea" :rows="4" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('textarea', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input v-model="form.[field.nameLower]" type="textarea" :rows="4" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('switch', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-switch v-model="form.[field.nameLower]" /></el-form-item>', empty_script),
    ('switch', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-switch v-model="form.[field.nameLower]" /></el-form-item>', empty_script),
    ('select', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-select v-model="form.[field.nameLower]" style="width:100%" clearable :placeholder="$t(\'[field.description]\')"><el-option v-for="d in [field.dictDataUrl]" :key="d.value" :label="d.label" :value="d.value" /></el-select></el-form-item>', empty_script),
    ('select', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-select v-model="form.[field.nameLower]" style="width:100%" clearable :placeholder="$t(\'[field.description]\')"><el-option v-for="d in [field.dictDataUrl]" :key="d.value" :label="d.label" :value="d.value" /></el-select></el-form-item>', empty_script),
    ('date', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="form.[field.nameLower]" type="date" style="width:100%" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('date', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="form.[field.nameLower]" type="date" style="width:100%" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('datetime', 'add', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="form.[field.nameLower]" type="datetime" style="width:100%" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('datetime', 'edit', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="form.[field.nameLower]" type="datetime" style="width:100%" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    # search controls
    ('search-input', 'search', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-input v-model="queryParams.[field.nameLower]" :placeholder="$t(\'[field.description]\')" clearable /></el-form-item>', empty_script),
    ('select', 'search', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-select v-model="queryParams.[field.nameLower]" clearable :placeholder="$t(\'[field.description]\')"><el-option v-for="d in [field.dictDataUrl]" :key="d.value" :label="d.label" :value="d.value" /></el-select></el-form-item>', empty_script),
    ('date', 'search', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="queryParams.[field.nameLower]" type="date" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    ('datetime', 'search', '<el-form-item :label="$t(\'[field.description]\')" prop="[field.nameLower]" data-gen-id="gen_field_[field.name]"><el-date-picker v-model="queryParams.[field.nameLower]" type="datetime" :placeholder="$t(\'[field.description]\')" /></el-form-item>', empty_script),
    # list columns
    ('table-column', 'list', '<el-table-column prop="[field.nameLower]" :label="$t(\'[field.description]\')" />', empty_script),
    # detail controls
    ('input', 'detail', '<el-descriptions-item :label="$t(\'[field.description]\')">{{ detail.[field.nameLower] }}</el-descriptions-item>', empty_script),
]

for i, (ct, ps, html, sc) in enumerate(controls):
    c = db.execute('SELECT id FROM sys_field_control_templates WHERE control_type=? AND page_section=? AND is_deleted=0', (ct, ps)).fetchone()
    if not c:
        db.execute('INSERT INTO sys_field_control_templates (control_type, page_section, html_content, script_sections, sort, create_time, is_deleted) VALUES (?,?,?,?,?,?,0)',
            (ct, ps, html, json.dumps(sc, ensure_ascii=False), i+1, now))

print(f'Control templates: {len(controls)} inserted')
db.commit()
db.close()
print('All seed data done!')
