import sqlite3, json

db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')

# Fix all \\$t → $t in script_sections
for table in ['sys_page_templates', 'sys_child_templates', 'sys_field_control_templates']:
    rows = db.execute(f'SELECT id, script_sections FROM {table} WHERE is_deleted=0').fetchall()
    for rid, ss in rows:
        if ss and r'\$t' in ss:
            ss = ss.replace(r'\$t', '$t')
            db.execute(f'UPDATE {table} SET script_sections=? WHERE id=?', (ss, rid))
            print(f'Fixed {table} id={rid}')

print('$t fix done')

# Now fix API import: change 'default':'use[gen.entityName]Api' to 'default':'* as [gen.entityNameLower]Api'
r = db.execute("SELECT id, script_sections FROM sys_page_templates WHERE page_type='index' AND is_deleted=0").fetchone()
if r:
    s = json.loads(r[1])
    for imp in s.get('imports', []):
        if isinstance(imp.get('default'), str) and 'use[gen.entityName]Api' in imp['default']:
            imp['default'] = '* as [gen.entityNameLower]Api'
            print(f'Fixed API import: {imp}')
    db.execute('UPDATE sys_page_templates SET script_sections=? WHERE id=?', (json.dumps(s, ensure_ascii=False), r[0]))

# Also fix add/edit detail imports (if they have API import)
for pt in ['add', 'edit', 'detail']:
    r = db.execute(f"SELECT id, script_sections FROM sys_page_templates WHERE page_type='{pt}' AND is_deleted=0").fetchone()
    if r:
        s = json.loads(r[1])
        for imp in s.get('imports', []):
            if isinstance(imp.get('default'), str) and 'use[gen.entityName]Api' in imp['default']:
                imp['default'] = '* as [gen.entityNameLower]Api'
                print(f'Fixed {pt} API import')
        db.execute(f'UPDATE sys_page_templates SET script_sections=? WHERE id=?', (json.dumps(s, ensure_ascii=False), r[0]))

db.commit()
db.close()
print('All fixes done')
