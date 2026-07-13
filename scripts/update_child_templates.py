#!/usr/bin/env python3
"""Update CodeGen templates in the configured SQLite database.

This script backs up the database before writing, then upserts the add/edit
card+dialog templates and the detail card template to the marker-driven
relation format. It also migrates field-control data-gen-id templates from
field names to field IDs.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
import time
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = REPO_ROOT / "CodeMaster.WebApi" / "appsettings.json"


EMPTY_SCRIPT = json.dumps(
    {
        "imports": [],
        "uses": [],
        "refs": [],
        "reactives": [],
        "functions": [],
        "hooks": [],
        "computed": [],
        "watches": [],
    },
    separators=(",", ":"),
)

SELECT_LABEL_BODY = [
    "if (val === undefined || val === null || val === '') return '-'",
    "if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val",
    "const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)",
    "const pick = (item, f) => item[f]",
    "const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }",
    "if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')",
    "if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')",
    "return resolve(val)",
]

CARD_HTML = (
    '<el-card shadow="never" style="margin-top:20px" data-gen-id="gen_child_[relation.entityName]">'
    '<template #header><div class="card-header"><span>{{ $t(\'[relation.entityTitleKey]\') }}</span>'
    '<el-button type="primary" size="small" @click="handleAdd[relation.entityName]">{{ $t(\'add\') }}</el-button>'
    '</div></template>'
    '<el-table :data="(form.[relation.collectionName] || []).filter(i => i.rowStatus !== 3)" border stripe>'
    '[relation.tableColumns]'
    '<el-table-column :label="$t(\'operation\')" width="140"><template #default="scope">'
    '<el-button link type="primary" size="small" @click="handleEdit[relation.entityName](scope.row, scope.$index)">{{ $t(\'edit\') }}</el-button>'
    '<el-button link type="danger" size="small" @click="handleRemove[relation.entityName](scope.$index)">{{ $t(\'delete\') }}</el-button>'
    '</template></el-table-column></el-table></el-card>'
)

DIALOG_HTML = (
    '<el-dialog v-model="[relation.dialogVisibleName]" '
    ':title="$t(\'[relation.entityTitleKey]\') + ([relation.editingIndexName] >= 0 ? $t(\'edit\') : $t(\'add\'))" '
    'width="700px">'
    '<el-form ref="[relation.formRefName]" :model="[relation.formName]" label-width="100px">'
    '<el-row :gutter="16">[relation.dialogColumns]</el-row></el-form>'
    '<template #footer>'
    '<el-button @click="[relation.dialogVisibleName] = false">{{ $t(\'cancel\') }}</el-button>'
    '<el-button type="primary" @click="handle[relation.entityName]Submit">{{ $t(\'confirm\') }}</el-button>'
    '</template></el-dialog>'
)

DETAIL_CARD_HTML = (
    '<div data-gen-id="gen_child_[relation.entityName]">'
    '<el-divider content-position="left">{{ $t(\'[relation.entityTitleKey]\') }}</el-divider>'
    '<el-table :data="detail.[relation.collectionName] || []" border stripe>'
    '[relation.tableColumns]'
    '</el-table></div>'
)

CARD_SCRIPT = json.dumps(
    {
        "imports": [{"path": "vue", "destructured": "ref, reactive, onMounted"}],
        "functions": [
            {
                "name": "handleAdd[relation.entityName]",
                "params": "",
                "body": [
                    "[relation.editingIndexName].value = -1;",
                    "Object.keys([relation.formName]).forEach(k => {",
                    "  if (typeof [relation.formName][k] === 'string') [relation.formName][k] = '';",
                    "  else if (typeof [relation.formName][k] === 'number') [relation.formName][k] = 0;",
                    "  else [relation.formName][k] = null;",
                    "});",
                    "[relation.formName].rowStatus = 1; // Added",
                    "[relation.dialogVisibleName].value = true;",
                ],
            },
            {
                "name": "handleEdit[relation.entityName]",
                "params": "row, index",
                "body": [
                    "[relation.editingIndexName].value = index;",
                    "Object.assign([relation.formName], row);",
                    "[relation.dialogVisibleName].value = true;",
                ],
            },
            {
                "name": "handleRemove[relation.entityName]",
                "params": "index",
                "body": [
                    "if (!form || !form.[relation.collectionName]) return;",
                    "const item = form.[relation.collectionName][index];",
                    "if (!item.id) { form.[relation.collectionName].splice(index, 1); }",
                    "else { item.rowStatus = 3; }",
                ],
            },
            {
                "name": "handle[relation.entityName]Submit",
                "params": "",
                "async": True,
                "body": [
                    "if (![relation.formRefName].value) return;",
                    "if (!form || !form.[relation.collectionName]) { [relation.dialogVisibleName].value = false; return; }",
                    "await [relation.formRefName].value.validate(async (valid) => {",
                    "  if (valid) {",
                    "    if ([relation.editingIndexName].value >= 0) {",
                    "      Object.assign(form.[relation.collectionName][[relation.editingIndexName].value], [relation.formName]);",
                    "      form.[relation.collectionName][[relation.editingIndexName].value].rowStatus = 2; // Modified",
                    "    } else {",
                    "      form.[relation.collectionName].push({ ... [relation.formName] });",
                    "    }",
                    "    [relation.dialogVisibleName].value = false;",
                    "  }",
                    "});",
                ],
            },
            {
                "name": "getSelectLabel",
                "params": "val, options, field",
                "body": [
                    "if (val === undefined || val === null || val === '') return '-'",
                    "if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val",
                    "const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)",
                    "const pick = (item, f) => item[f]",
                    "const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }",
                    "if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')",
                    "if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')",
                    "return resolve(val)",
                ],
            },
        ],
        "refs": [
            {"name": "[relation.formRefName]"},
            {"name": "[relation.editingIndexName]", "initialValue": "-1"},
            {"name": "[relation.dialogVisibleName]", "initialValue": "false"},
        ],
        "reactives": [{"name": "[relation.formName]", "fields": {}}],
    },
    separators=(",", ":"),
)


TEMPLATES = [
    ("add", "card", CARD_HTML, CARD_SCRIPT, 1),
    ("add", "dialog", DIALOG_HTML, EMPTY_SCRIPT, 2),
    ("edit", "card", CARD_HTML, CARD_SCRIPT, 1),
    ("edit", "dialog", DIALOG_HTML, EMPTY_SCRIPT, 2),
    ("detail", "card", DETAIL_CARD_HTML, EMPTY_SCRIPT, 1),
]


def resolve_db_path(config_path: Path, explicit_db: str | None) -> Path:
    if explicit_db:
        return Path(explicit_db).expanduser().resolve()

    config = json.loads(config_path.read_text(encoding="utf-8"))
    conn = config["ConnectionStrings"]["DefaultConnection"]
    parts = [p.strip() for p in conn.split(";") if p.strip()]
    values = {}
    for part in parts:
        if "=" in part:
            key, value = part.split("=", 1)
            values[key.strip().lower()] = value.strip()

    data_source = values.get("data source") or values.get("filename")
    if not data_source:
        raise ValueError(f"Unsupported connection string: {conn}")

    db_path = Path(data_source)
    if not db_path.is_absolute():
        db_path = (config_path.parent / db_path).resolve()
    return db_path


def ensure_table(conn: sqlite3.Connection) -> None:
    exists = conn.execute(
        "select 1 from sqlite_master where type='table' and name='sys_page_templates'"
    ).fetchone()
    if not exists:
        raise RuntimeError("Table sys_page_templates does not exist")

    exists = conn.execute(
        "select 1 from sqlite_master where type='table' and name='sys_child_templates'"
    ).fetchone()
    if not exists:
        raise RuntimeError("Table sys_child_templates does not exist")

    exists = conn.execute(
        "select 1 from sqlite_master where type='table' and name='sys_field_control_templates'"
    ).fetchone()
    if not exists:
        raise RuntimeError("Table sys_field_control_templates does not exist")


def next_id(index: int) -> int:
    return int(time.time() * 1000) * 1000 + index


def upsert_template(
    conn: sqlite3.Connection,
    page_type: str,
    child_type: str,
    html: str,
    script: str,
    sort: int,
    index: int,
) -> str:
    now = datetime.now().isoformat(timespec="seconds")
    row = conn.execute(
        """
        select id from sys_child_templates
        where page_type = ? and child_type = ? and is_deleted = 0
        order by sort, id
        limit 1
        """,
        (page_type, child_type),
    ).fetchone()

    if row:
        conn.execute(
            """
            update sys_child_templates
            set html_content = ?, script_sections = ?, sort = ?, update_time = ?
            where id = ?
            """,
            (html, script, sort, now, row[0]),
        )
        return f"updated {page_type}/{child_type} id={row[0]}"

    new_id = next_id(index)
    conn.execute(
        """
        insert into sys_child_templates
        (id, page_type, child_type, html_content, script_sections, sort,
         create_time, is_deleted)
        values (?, ?, ?, ?, ?, ?, ?, 0)
        """,
        (new_id, page_type, child_type, html, script, sort, now),
    )
    return f"inserted {page_type}/{child_type} id={new_id}"


def update_field_control_gen_ids(conn: sqlite3.Connection) -> int:
    rows = conn.execute(
        """
        select id, html_content
        from sys_field_control_templates
        where is_deleted = 0
          and (
            html_content like '%data-gen-id="gen_field_[field.name]"%'
            or html_content like '%data-gen-id="gen_col_[field.name]"%'
            or html_content like '%data-gen-id="gen_search_[field.name]"%'
          )
        """
    ).fetchall()

    updated = 0
    now = datetime.now().isoformat(timespec="seconds")
    for row_id, html in rows:
        new_html = (
            html.replace('data-gen-id="gen_field_[field.name]"', 'data-gen-id="gen_field_[field.id]"')
                .replace('data-gen-id="gen_col_[field.name]"', 'data-gen-id="gen_col_[field.id]"')
                .replace('data-gen-id="gen_search_[field.name]"', 'data-gen-id="gen_field_[field.id]"')
        )
        if new_html == html:
            continue
        conn.execute(
            """
            update sys_field_control_templates
            set html_content = ?, update_time = ?
            where id = ?
            """,
            (new_html, now, row_id),
        )
        updated += 1

    return updated


def update_field_control_missing_gen_ids(conn: sqlite3.Connection) -> int:
    rows = conn.execute(
        """
        select id, html_content
        from sys_field_control_templates
        where is_deleted = 0
          and (
            page_section = 'detail'
            or (control_type = 'select-table' and page_section = 'search')
          )
        """
    ).fetchall()

    updated = 0
    now = datetime.now().isoformat(timespec="seconds")
    for row_id, html in rows:
        if 'data-gen-id=' in html:
            continue

        new_html = html
        if new_html.startswith('<el-descriptions-item '):
            new_html = new_html.replace(
                '<el-descriptions-item :label="$t(\'[field.labelKey]\')">',
                '<el-descriptions-item :label="$t(\'[field.labelKey]\')" data-gen-id="gen_field_[field.id]">',
                1,
            )
        elif new_html.startswith('<el-form-item '):
            new_html = new_html.replace(
                '<el-form-item :label="$t(\'[field.relatedDisplayLabel]\')">',
                '<el-form-item :label="$t(\'[field.relatedDisplayLabel]\')" data-gen-id="gen_field_[field.id]">',
                1,
            )

        if new_html == html:
            continue
        conn.execute(
            """
            update sys_field_control_templates
            set html_content = ?, update_time = ?
            where id = ?
            """,
            (new_html, now, row_id),
        )
        updated += 1

    return updated


def update_i18n_template_keys(conn: sqlite3.Connection) -> int:
    replacements = (
        ("[field.description]", "[field.labelKey]"),
        ("[relation.entityNameAllLower]", "[relation.entityTitleKey]"),
        ("{{ $t('add') }} [gen.entityDescription]", "{{ $t('add') }} {{ $t('[gen.entityTitleKey]') }}"),
        ("{{ $t('edit') }} [gen.entityDescription]", "{{ $t('edit') }} {{ $t('[gen.entityTitleKey]') }}"),
        ("[gen.entityDescription] {{ $t('detail') }}", "{{ $t('[gen.entityTitleKey]') }} {{ $t('detail') }}"),
        ("$t('[field.relatedDisplayLabel]')", "$t('[field.labelKey]')"),
    )
    tables = (
        ("sys_page_templates", ("html_content", "script_sections")),
        ("sys_child_templates", ("html_content", "script_sections")),
        ("sys_field_control_templates", ("html_content", "script_sections")),
    )

    updated = 0
    now = datetime.now().isoformat(timespec="seconds")
    for table, columns in tables:
        rows = conn.execute(
            f"select id, {', '.join(columns)} from {table} where is_deleted = 0"
        ).fetchall()
        for row in rows:
            row_id = row[0]
            values = list(row[1:])
            new_values = []
            changed = False
            for value in values:
                new_value = value
                if new_value is not None:
                    for old, new in replacements:
                        new_value = new_value.replace(old, new)
                changed = changed or new_value != value
                new_values.append(new_value)
            if not changed:
                continue

            assignments = ", ".join(f"{column} = ?" for column in columns)
            conn.execute(
                f"update {table} set {assignments}, update_time = ? where id = ?",
                (*new_values, now, row_id),
            )
            updated += 1

    return updated


def update_select_label_scripts(conn: sqlite3.Connection) -> int:
    tables = ("sys_page_templates", "sys_child_templates")
    updated = 0
    now = datetime.now().isoformat(timespec="seconds")

    for table in tables:
        rows = conn.execute(
            f"select id, script_sections from {table} where is_deleted = 0 and script_sections like '%getSelectLabel%'"
        ).fetchall()
        for row_id, script in rows:
            try:
                data = json.loads(script)
            except Exception:
                continue

            changed = False
            for fn in data.get("functions", []):
                if fn.get("name") == "getSelectLabel" and fn.get("body") != SELECT_LABEL_BODY:
                    fn["body"] = SELECT_LABEL_BODY
                    changed = True

            if not changed:
                continue

            conn.execute(
                f"update {table} set script_sections = ?, update_time = ? where id = ?",
                (json.dumps(data, ensure_ascii=False, separators=(",", ":")), now, row_id),
            )
            updated += 1

    return updated


def update_select_multiple_and_display_templates(conn: sqlite3.Connection) -> int:
    replacements = (
        (
            '<el-select v-model="[field.formPrefix].[field.nameLower]" style="width:100%" clearable :placeholder="$t(\'[field.labelKey]\')">',
            '<el-select v-model="[field.formPrefix].[field.nameLower][field.multipleSuffix]" style="width:100%" clearable [field.multipleAttr] :placeholder="$t(\'[field.labelKey]\')">',
        ),
        (
            '<el-select v-model="[field.formPrefix].[field.nameLower]" clearable :placeholder="$t(\'[field.labelKey]\')">',
            '<el-select v-model="[field.formPrefix].[field.nameLower]" clearable [field.multipleAttr] :placeholder="$t(\'[field.labelKey]\')">',
        ),
        (
            "getSelectLabel(detail.[field.nameLower], [field.relatedEntityNameLower]Options, '[field.relatedDisplayLabel]')",
            "getSelectLabel(detail.[field.nameLower], [field.relatedEntityNameLower]Options, '[field.relatedDisplayFieldsArg]')",
        ),
    )

    rows = conn.execute(
        """
        select id, html_content
        from sys_field_control_templates
        where is_deleted = 0
        """
    ).fetchall()

    updated = 0
    now = datetime.now().isoformat(timespec="seconds")
    for row_id, html in rows:
        new_html = html
        for old, new in replacements:
            new_html = new_html.replace(old, new)
        if new_html == html:
            continue

        conn.execute(
            """
            update sys_field_control_templates
            set html_content = ?, update_time = ?
            where id = ?
            """,
            (new_html, now, row_id),
        )
        updated += 1

    return updated


def update_detail_page_template_import(conn: sqlite3.Connection) -> int:
    row = conn.execute(
        """
        select id, script_sections
        from sys_page_templates
        where is_deleted = 0 and page_type = 'detail'
        limit 1
        """
    ).fetchone()
    if not row:
        return 0

    row_id, script = row
    new_script = script.replace(
        '"destructured":"ref, onMounted"',
        '"destructured":"ref, reactive, onMounted"',
    ).replace(
        '"destructured": "ref, onMounted"',
        '"destructured": "ref, reactive, onMounted"',
    )
    if new_script == script:
        return 0

    conn.execute(
        """
        update sys_page_templates
        set script_sections = ?, update_time = ?
        where id = ?
        """,
        (new_script, datetime.now().isoformat(timespec="seconds"), row_id),
    )
    return 1


def update_index_detail_action(conn: sqlite3.Connection) -> int:
    row = conn.execute(
        """
        select id, html_content, script_sections
        from sys_page_templates
        where is_deleted = 0 and page_type = 'index'
        limit 1
        """
    ).fetchone()
    if not row:
        return 0

    row_id, html, script = row
    new_html = html
    if "handleDetail(scope.row)" not in new_html:
        new_html = new_html.replace(
            '<el-table-column :label="$t(\'operation\')" width="180" fixed="right" data-gen-id="gen_operations">',
            '<el-table-column :label="$t(\'operation\')" width="220" fixed="right" data-gen-id="gen_operations">',
        ).replace(
            '<el-table-column :label="$t(\'operation\')" width="200" fixed="right" data-gen-id="gen_operations">',
            '<el-table-column :label="$t(\'operation\')" width="220" fixed="right" data-gen-id="gen_operations">',
        )
        new_html = new_html.replace(
            '<el-button link type="primary" @click="handleEdit(scope.row)">{{ $t(\'edit\') }}</el-button>',
            '<el-button link type="primary" @click="handleDetail(scope.row)">{{ $t(\'detail\') }}</el-button>\n        <el-button link type="primary" @click="handleEdit(scope.row)">{{ $t(\'edit\') }}</el-button>',
            1,
        )

    new_script = script
    if '"name":"handleDetail"' not in new_script and '"name": "handleDetail"' not in new_script:
        try:
            data = json.loads(script)
            functions = data.setdefault("functions", [])
            detail_fn = {
                "name": "handleDetail",
                "params": "row",
                "body": ["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/detail?id=${row.id}`);"],
            }
            insert_at = next((i for i, fn in enumerate(functions) if fn.get("name") == "handleEdit"), len(functions))
            functions.insert(insert_at, detail_fn)
            new_script = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
        except Exception:
            new_script = new_script.replace(
                '{"name":"handleEdit","params":"row","body":["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/edit?id=${row.id}`);"]}',
                '{"name":"handleDetail","params":"row","body":["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/detail?id=${row.id}`);"]},{"name":"handleEdit","params":"row","body":["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/edit?id=${row.id}`);"]}',
            ).replace(
                '{"name": "handleEdit", "params": "row", "body": ["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/edit?id=${row.id}`);"]}',
                '{"name": "handleDetail", "params": "row", "body": ["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/detail?id=${row.id}`);"]}, {"name": "handleEdit", "params": "row", "body": ["router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/edit?id=${row.id}`);"]}',
            )

    if new_html == html and new_script == script:
        return 0

    conn.execute(
        """
        update sys_page_templates
        set html_content = ?, script_sections = ?, update_time = ?
        where id = ?
        """,
        (new_html, new_script, datetime.now().isoformat(timespec="seconds"), row_id),
    )
    return 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", default=str(DEFAULT_CONFIG), help="appsettings.json path")
    parser.add_argument("--db", help="SQLite db path; overrides --config")
    parser.add_argument("--no-backup", action="store_true", help="Skip database backup")
    args = parser.parse_args()

    config_path = Path(args.config).expanduser().resolve()
    db_path = resolve_db_path(config_path, args.db)
    if not db_path.exists():
        raise FileNotFoundError(db_path)

    if not args.no_backup:
        backup = db_path.with_name(f"{db_path.name}.bak_{datetime.now().strftime('%Y%m%d%H%M%S')}")
        shutil.copy2(db_path, backup)
        print(f"backup: {backup}")

    with sqlite3.connect(db_path) as conn:
        ensure_table(conn)
        conn.execute("begin")
        try:
            for i, item in enumerate(TEMPLATES, start=1):
                print(upsert_template(conn, *item, index=i))
            print(f"updated field control gen ids: {update_field_control_gen_ids(conn)}")
            print(f"updated i18n template keys: {update_i18n_template_keys(conn)}")
            print(f"updated select label scripts: {update_select_label_scripts(conn)}")
            print(f"updated select multiple/display templates: {update_select_multiple_and_display_templates(conn)}")
            print(f"added missing field control gen ids: {update_field_control_missing_gen_ids(conn)}")
            print(f"updated detail page imports: {update_detail_page_template_import(conn)}")
            print(f"updated index detail action: {update_index_detail_action(conn)}")
            conn.commit()
        except Exception:
            conn.rollback()
            raise

    print(f"done: {db_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
