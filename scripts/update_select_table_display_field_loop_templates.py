#!/usr/bin/env python3
"""Update select-table templates to use bracketed display field loop markers."""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = REPO_ROOT / "CodeMaster.Migrator" / "appsettings.json"

SELECT_TABLE_SCRIPT = json.dumps(
    {
        "imports": [
            {
                "path": "@/api/[field.relatedModuleNameLower]/[field.relatedEntityNameLower]",
                "destructured": "getList as get[field.relatedEntityName]List",
            }
        ],
        "uses": [],
        "refs": [{"name": "[field.relatedEntityNameLower]Options", "initialValue": "[]"}],
        "reactives": [],
        "functions": [],
        "hooks": [
            {
                "name": "onMounted",
                "body": [
                    "get[field.relatedEntityName]List().then(res => { [field.relatedEntityNameLower]Options.value = res.map(item => ({ value: item.[field.relatedEntityIdFieldLower], ...item })) })"
                ],
            }
        ],
        "computed": [],
        "watches": [],
    },
    ensure_ascii=False,
    separators=(",", ":"),
)

TEMPLATES = {
    "add": (
        '<el-col [field.col]><el-form-item :label="$t(\'[field.labelKey]\')" '
        'prop="[field.nameLower][field.multipleSuffix]" data-gen-id="gen_field_[field.id]">'
        '<el-select v-model="[field.formPrefix].[field.nameLower][field.multipleSuffix]" '
        'style="width:100%" clearable [field.multipleAttr] :placeholder="$t(\'[field.labelKey]\')">'
        '<el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" '
        ':label="[field.relatedDisplayLabelExpr]" :value="item.value">'
        '<span [v-for="displayField in field.displayFields"] style="margin-right:8px">{{ [displayField.itemValueExpr] }}</span>'
        "</el-option></el-select></el-form-item></el-col>"
    ),
    "edit": (
        '<el-col [field.col]><el-form-item :label="$t(\'[field.labelKey]\')" '
        'prop="[field.nameLower][field.multipleSuffix]" data-gen-id="gen_field_[field.id]">'
        '<el-select v-model="[field.formPrefix].[field.nameLower][field.multipleSuffix]" '
        'style="width:100%" clearable [field.multipleAttr] :placeholder="$t(\'[field.labelKey]\')">'
        '<el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" '
        ':label="[field.relatedDisplayLabelExpr]" :value="item.value">'
        '<span [v-for="displayField in field.displayFields"] style="margin-right:8px">{{ [displayField.itemValueExpr] }}</span>'
        "</el-option></el-select></el-form-item></el-col>"
    ),
    "search": (
        '<el-form-item :label="$t(\'[field.labelKey]\')" data-gen-id="gen_field_[field.id]">'
        '<el-select v-model="[field.formPrefix].[field.nameLower]" clearable [field.multipleAttr] '
        ':placeholder="$t(\'[field.labelKey]\')">'
        '<el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" '
        ':label="[field.relatedDisplayLabelExpr]" :value="item.value">'
        '<span [v-for="displayField in field.displayFields"] style="margin-right:8px">{{ [displayField.itemValueExpr] }}</span>'
        "</el-option></el-select></el-form-item>"
    ),
    "list": (
        '<el-table-column [v-for="displayField in field.displayFields"] '
        ':label="$t(\'[displayField.labelKey]\')" data-gen-id="gen_col_[field.id]_[displayField.nameLower]">'
        '<template #default="scope">{{ getSelectLabel(scope.row.[field.nameLower], '
        "[field.relatedEntityNameLower]Options, '[displayField.nameLower]') }}</template></el-table-column>"
    ),
    "detail": (
        '<el-descriptions-item [v-for="displayField in field.displayFields"] '
        ':label="$t(\'[displayField.labelKey]\')" data-gen-id="gen_field_[field.id]_[displayField.nameLower]">'
        "{{ getSelectLabel(detail.[field.nameLower], [field.relatedEntityNameLower]Options, "
        "'[displayField.nameLower]') }}</el-descriptions-item>"
    ),
}

SORTS = {"list": 20, "add": 21, "edit": 22, "search": 34, "detail": 35}


def resolve_db_path(config_path: Path, explicit_db: str | None) -> Path:
    if explicit_db:
        return Path(explicit_db).expanduser().resolve()

    config = json.loads(config_path.read_text(encoding="utf-8"))
    conn = config["ConnectionStrings"]["DefaultConnection"]
    values: dict[str, str] = {}
    for part in [p.strip() for p in conn.split(";") if p.strip()]:
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
        "select 1 from sqlite_master where type='table' and name='sys_field_control_templates'"
    ).fetchone()
    if not exists:
        raise RuntimeError("Table sys_field_control_templates does not exist")


def get_columns(conn: sqlite3.Connection) -> list[str]:
    return [row[1] for row in conn.execute("pragma table_info(sys_field_control_templates)")]


def next_id(conn: sqlite3.Connection) -> int:
    current = conn.execute("select max(id) from sys_field_control_templates").fetchone()[0]
    return int(current or 0) + 1


def upsert_template(conn: sqlite3.Connection, columns: list[str], page_section: str, html: str) -> str:
    now = datetime.now().isoformat(timespec="seconds")
    row = conn.execute(
        """
        select id from sys_field_control_templates
        where is_deleted = 0 and control_type = 'select-table' and page_section = ?
        limit 1
        """,
        (page_section,),
    ).fetchone()

    if row:
        conn.execute(
            """
            update sys_field_control_templates
            set html_content = ?, script_sections = ?, sort = ?, update_time = ?
            where id = ?
            """,
            (html, SELECT_TABLE_SCRIPT, SORTS[page_section], now, row[0]),
        )
        return "updated"

    values = {
        "id": next_id(conn),
        "control_type": "select-table",
        "page_section": page_section,
        "html_content": html,
        "script_sections": SELECT_TABLE_SCRIPT,
        "sort": SORTS[page_section],
        "is_deleted": 0,
        "create_time": now,
        "update_time": now,
    }
    insert_columns = [col for col in columns if col in values]
    placeholders = ",".join(["?"] * len(insert_columns))
    conn.execute(
        f"insert into sys_field_control_templates ({','.join(insert_columns)}) values ({placeholders})",
        [values[col] for col in insert_columns],
    )
    return "inserted"


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
        columns = get_columns(conn)
        conn.execute("begin")
        try:
            results = {
                section: upsert_template(conn, columns, section, html)
                for section, html in TEMPLATES.items()
            }
            conn.commit()
        except Exception:
            conn.rollback()
            raise

    for section, action in results.items():
        print(f"{action} select-table/{section}")
    print(f"done: {db_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
