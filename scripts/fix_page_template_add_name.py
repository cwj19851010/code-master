#!/usr/bin/env python3
"""Fix the configured SQLite DB page template name for the add page."""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = REPO_ROOT / "CodeMaster.Migrator" / "appsettings.json"


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
        "select 1 from sqlite_master where type='table' and name='sys_page_templates'"
    ).fetchone()
    if not exists:
        raise RuntimeError("Table sys_page_templates does not exist")


def fix_add_name(conn: sqlite3.Connection) -> str:
    now = datetime.now().isoformat(timespec="seconds")
    row = conn.execute(
        """
        select id, name from sys_page_templates
        where is_deleted = 0 and page_type = 'add'
        limit 1
        """
    ).fetchone()
    if not row:
        raise RuntimeError("Add page template does not exist")

    if row[1] == "新增页":
        return "unchanged"

    conn.execute(
        """
        update sys_page_templates
        set name = ?, update_time = ?
        where id = ?
        """,
        ("新增页", now, row[0]),
    )
    return f"updated from {row[1]!r}"


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
        conn.execute("pragma busy_timeout = 5000")
        ensure_table(conn)
        conn.execute("begin")
        try:
            action = fix_add_name(conn)
            conn.commit()
        except Exception:
            conn.rollback()
            raise

    print(f"{action} page template add name")
    print(f"done: {db_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
