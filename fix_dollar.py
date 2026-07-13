import sqlite3, json

db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')

# Fix: replace literal backslash-dollar-t with dollar-t in all script_sections
for table in ['sys_page_templates', 'sys_child_templates', 'sys_field_control_templates']:
    rows = db.execute(f'SELECT id, script_sections FROM {table} WHERE is_deleted=0').fetchall()
    for rid, ss in rows:
        if ss and '\\$t' in ss:
            ss = ss.replace('\\$t', '$t')
            db.execute(f'UPDATE {table} SET script_sections=? WHERE id=?', (ss, rid))
            print(f'Fixed {table} id={rid}')
        # Also fix double-backslash in general
        if ss and '\\\\' in ss:
            old = ss
            ss = ss.replace('\\\\$', '$')
            if ss != old:
                db.execute(f'UPDATE {table} SET script_sections=? WHERE id=?', (ss, rid))
                print(f'Fixed double-backslash in {table} id={rid}')

db.commit()

# Verify
for table in ['sys_page_templates', 'sys_child_templates']:
    rows = db.execute(f'SELECT id, script_sections FROM {table} WHERE is_deleted=0').fetchall()
    for rid, ss in rows:
        if '\\$t' in ss:
            print(f'STILL HAS \\$t: {table} id={rid}')
            
print('Done - regenerating would fix $t issue')
db.close()
