"""
修复 sys_page_templates 和 sys_field_control_templates 中的模板数据，
使 DB 模板路径与 Scriban API 文件契约对齐。
"""
import sqlite3, json, re

db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')

# ========== 1. 修复页面模板脚本 ==========
print("=== 修复 sys_page_templates ===")
rows = db.execute('SELECT id, page_type, script_sections FROM sys_page_templates WHERE is_deleted=0').fetchall()

for rid, ptype, ss in rows:
    s = json.loads(ss)
    changed = False

    # 遍历所有 import，把 "* as use[gen.entityName]Api" 改成 "* as [gen.entityNameLower]Api"
    for imp in s.get('imports', []):
        if isinstance(imp.get('default'), str) and 'use[gen.entityName]Api' in imp['default']:
            imp['default'] = imp['default'].replace('use[gen.entityName]Api', '[gen.entityNameLower]Api')
            changed = True
            print(f'  [{ptype}] import: use[gen.entityName]Api -> [gen.entityNameLower]Api')

    # 遍历所有 function body，修复 API 调用
    for fn in s.get('functions', []):
        new_body = []
        for line in fn.get('body', []):
            orig = line

            # 1) use[gen.entityName]Api.xxx -> [gen.entityNameLower]Api.xxx
            line = line.replace('use[gen.entityName]Api.', '[gen.entityNameLower]Api.')

            # 2) getList() -> getPagedList() (for index page)
            line = line.replace('[gen.entityNameLower]Api.getList(', '[gen.entityNameLower]Api.getPagedList(')

            # 3) .delete( -> .deleteById(
            line = line.replace('[gen.entityNameLower]Api.delete(', '[gen.entityNameLower]Api.deleteById(')

            # 4) res.data.rows -> res.items (only in index getList)
            line = line.replace('res.data.rows', 'res.items')

            # 5) res.data.total -> res.total
            line = line.replace('res.data.total', 'res.total')

            # 6) Object.assign(form, res.data) -> Object.assign(form, res)
            line = line.replace('Object.assign(form, res.data)', 'Object.assign(form, res)')

            # 7) Object.assign(detail, res.data) -> Object.assign(detail, res)
            line = line.replace('Object.assign(detail, res.data)', 'Object.assign(detail, res)')

            if line != orig:
                changed = True
                print(f'  [{ptype}] {fn["name"]}: "{orig.strip()[:60]}..." -> "{line.strip()[:60]}..."')

            new_body.append(line)
        fn['body'] = new_body

    # 对于 index 页面的 getList，把 pageNum/pageSize 注入 params
    if ptype == 'index':
        for fn in s.get('functions', []):
            if fn['name'] == 'getList':
                new_body = []
                for line in fn['body']:
                    if 'getPagedList(queryParams)' in line or 'getPagedList(params)' in line:
                        # 把单行拆成多行，注入 pageNum/pageSize
                        line = line.replace(
                            'getPagedList(queryParams)',
                            'getPagedList({ ...queryParams, pageNum: pagination.page, pageSize: pagination.pageSize })'
                        )
                        # 如果已经是 params 形式，替换
                        line = line.replace(
                            'getPagedList(params)',
                            'getPagedList({ ...queryParams, pageNum: pagination.page, pageSize: pagination.pageSize })'
                        )
                    new_body.append(line)
                fn['body'] = new_body
                # 确保 getList 是拆成多行的（之前是一行 try-catch）
                if len(new_body) == 1 and 'try {' in new_body[0]:
                    old_line = new_body[0]
                    # 拆成多行：loading, try {, params, res, items, total, }, catch, finally
                    new_body = [
                        "loading.value = true;",
                        "try {",
                        "  const params = { ...queryParams, pageNum: pagination.page, pageSize: pagination.pageSize };",
                    ]
                    # 提取 API 调用行
                    api_match = re.search(r'const res = await (\[gen\.\w+\]Api\.\w+)\([^)]*\)', old_line)
                    if api_match:
                        new_body.append(f"  const res = await {api_match.group(1)}(params);")
                    new_body.extend([
                        "  tableData.value = res.items;",
                        "  pagination.total = res.total;",
                        "}",
                        "catch(e) { ElMessage.error(e.message); }",
                        "finally { loading.value = false; }"
                    ])
                    fn['body'] = new_body
                    changed = True
                    print(f'  [{ptype}] getList: 拆分为多行 + 注入分页参数')

    if changed:
        db.execute('UPDATE sys_page_templates SET script_sections=? WHERE id=?',
                   (json.dumps(s, ensure_ascii=False), rid))
        print(f'  -> 已保存 ID={rid}')

# ========== 2. 修复控件模板 ==========
print("\n=== 修复 sys_field_control_templates ===")
rows = db.execute('SELECT id, control_type, page_section, html_content FROM sys_field_control_templates WHERE is_deleted=0').fetchall()

for rid, ctype, ps, html in rows:
    orig = html
    changed = False

    # 搜索控件：添加 prop="[field.nameLower]"
    if ps == 'search' and 'prop=' not in html:
        html = html.replace(
            ':label="$t(\'[field.description]\')"',
            ':label="$t(\'[field.description]\')" prop="[field.nameLower]"'
        )
        # 也处理双引号版本
        html = html.replace(
            ':label="$t([field.description])"',
            ':label="$t([field.description])" prop="[field.nameLower]"'
        )
        # C# escaped version
        html = html.replace(
            ':label=\\"$t(\\\'[field.description]\\\')\\"',
            ':label=\\"$t(\\\'[field.description]\\\')\\" prop=\\"[field.nameLower]\\"'
        )

    # table-column：移除 [field.isRequired]
    if ctype == 'table-column' and '[field.isRequired]' in html:
        html = html.replace(' [field.isRequired]', '')
        html = html.replace('[field.isRequired] ', '')
        html = html.replace('[field.isRequired]', '')

    if html != orig:
        changed = True
        print(f'  [{ctype}/{ps}] 已修复')

    if changed:
        db.execute('UPDATE sys_field_control_templates SET html_content=? WHERE id=?', (html, rid))

db.commit()
db.close()
print("\n=== 全部修复完成 ===")
