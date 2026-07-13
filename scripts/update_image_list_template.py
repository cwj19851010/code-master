#!/usr/bin/env python3
"""Update image list template in the configured SQLite DB."""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = REPO_ROOT / "CodeMaster.Migrator" / "appsettings.json"

IMAGE_LIST_HTML = (
    '<el-table-column [field.prop] :label="$t(\'[field.labelKey]\')" width="100" '
    'data-gen-id="gen_col_[field.id]"><template #default="scope">'
    '<el-image v-if="scope.row.[field.nameLower]" :src="scope.row.[field.nameLower]" '
    'style="width:48px;height:48px;border-radius:4px" fit="cover" '
    ':preview-src-list="[scope.row.[field.nameLower]]" preview-teleported />'
    "<span v-else>-</span></template></el-table-column>"
)

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
    ensure_ascii=False,
    separators=(",", ":"),
)


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


def upsert_image_list(conn: sqlite3.Connection) -> str:
    now = datetime.now().isoformat(timespec="seconds")
    row = conn.execute(
        """
        select id from sys_field_control_templates
        where is_deleted = 0 and control_type = 'image' and page_section = 'list'
        limit 1
        """
    ).fetchone()

    if row:
        conn.execute(
            """
            update sys_field_control_templates
            set html_content = ?, script_sections = ?, sort = ?, update_time = ?
            where id = ?
            """,
            (IMAGE_LIST_HTML, EMPTY_SCRIPT, 20, now, row[0]),
        )
        return "updated"

    columns = get_columns(conn)
    values = {
        "id": next_id(conn),
        "control_type": "image",
        "page_section": "list",
        "html_content": IMAGE_LIST_HTML,
        "script_sections": EMPTY_SCRIPT,
        "sort": 20,
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
        conn.execute("begin")
        try:
            action = upsert_image_list(conn)
            conn.commit()
        except Exception:
            conn.rollback()
            raise

    print(f"{action} image list template")
    print(f"done: {db_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
