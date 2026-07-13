#!/usr/bin/env python3
"""Update upload templates for generated pages in the configured SQLite DB."""

from __future__ import annotations

import argparse
import json
import shutil
import sqlite3
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = REPO_ROOT / "CodeMaster.Migrator" / "appsettings.json"


FILE_SCRIPT = json.dumps(
    {
        "imports": [
            {"path": "element-plus", "destructured": "ElMessage"},
            {"path": "@/api/system/file", "destructured": "upload"},
        ],
        "uses": [],
        "refs": [],
        "reactives": [],
        "functions": [
            {
                "name": "uploadFile",
                "params": "opt",
                "async": True,
                "body": [
                    "try {",
                    "  const res = await upload(opt.file);",
                    "  const url = res?.accessUrl || res?.AccessUrl || res?.fileUrl || res?.FileUrl || res;",
                    "  opt.onSuccess(url);",
                    "} catch (e) {",
                    "  opt.onError(e);",
                    "}",
                ],
            },
            {
                "name": "beforeFileUpload",
                "params": "file",
                "body": [
                    "const isLt10M = file.size / 1024 / 1024 < 10;",
                    "if (!isLt10M) { ElMessage.error('File size cannot exceed 10MB'); return false; }",
                    "return true;",
                ],
            },
        ],
        "hooks": [],
        "computed": [],
        "watches": [],
    },
    ensure_ascii=False,
    separators=(",", ":"),
)

IMAGE_SCRIPT = json.dumps(
    {
        "imports": [
            {"path": "element-plus", "destructured": "ElMessage"},
            {"path": "@element-plus/icons-vue", "destructured": "Plus"},
            {"path": "@/api/system/file", "destructured": "upload"},
        ],
        "uses": [],
        "refs": [],
        "reactives": [],
        "functions": [
            {
                "name": "uploadFile",
                "params": "opt",
                "async": True,
                "body": [
                    "try {",
                    "  const res = await upload(opt.file);",
                    "  const url = res?.accessUrl || res?.AccessUrl || res?.fileUrl || res?.FileUrl || res;",
                    "  opt.onSuccess(url);",
                    "} catch (e) {",
                    "  opt.onError(e);",
                    "}",
                ],
            },
            {
                "name": "beforeImageUpload",
                "params": "file",
                "body": [
                    "const isImage = file.type.startsWith('image/');",
                    "if (!isImage) { ElMessage.error('Only image files are allowed'); return false; }",
                    "const isLt2M = file.size / 1024 / 1024 < 2;",
                    "if (!isLt2M) { ElMessage.error('File size cannot exceed 2MB'); return false; }",
                    "return true;",
                ],
            },
        ],
        "hooks": [],
        "computed": [],
        "watches": [],
    },
    ensure_ascii=False,
    separators=(",", ":"),
)

FILE_HTML = (
    '<el-col [field.col]><el-form-item :label="$t(\'[field.labelKey]\')" [field.prop] '
    'data-gen-id="gen_field_[field.id]"><el-upload :http-request="uploadFile" '
    ':before-upload="beforeFileUpload" :on-success="(url) => [field.formPrefix].[field.nameLower] = url" '
    ':file-list="[field.formPrefix].[field.nameLower] ? [{ url: [field.formPrefix].[field.nameLower] }] : []" '
    'list-type="picture" :limit="1"><el-button size="small" type="primary">{{ $t(\'upload\') }}</el-button>'
    '</el-upload></el-form-item></el-col>'
)

IMAGE_HTML = (
    '<el-col [field.col]><el-form-item :label="$t(\'[field.labelKey]\')" [field.prop] '
    'data-gen-id="gen_field_[field.id]"><el-upload :http-request="uploadFile" '
    ':before-upload="beforeImageUpload" :on-success="(url) => [field.formPrefix].[field.nameLower] = url" '
    ':file-list="[field.formPrefix].[field.nameLower] ? [{ url: [field.formPrefix].[field.nameLower] }] : []" '
    'list-type="picture-card" :limit="1"><el-icon><Plus /></el-icon></el-upload></el-form-item></el-col>'
)


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


def ensure_tables(conn: sqlite3.Connection) -> None:
    for table in ("sys_field_control_templates",):
        exists = conn.execute(
            "select 1 from sqlite_master where type='table' and name=?", (table,)
        ).fetchone()
        if not exists:
            raise RuntimeError(f"Table {table} does not exist")


def update_control(conn: sqlite3.Connection, control_type: str, page_section: str, html: str, script: str) -> int:
    now = datetime.now().isoformat(timespec="seconds")
    cur = conn.execute(
        """
        update sys_field_control_templates
        set html_content = ?, script_sections = ?, update_time = ?
        where is_deleted = 0 and control_type = ? and page_section = ?
        """,
        (html, script, now, control_type, page_section),
    )
    return cur.rowcount


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
        ensure_tables(conn)
        conn.execute("begin")
        try:
            total = 0
            total += update_control(conn, "file", "add", FILE_HTML, FILE_SCRIPT)
            total += update_control(conn, "file", "edit", FILE_HTML, FILE_SCRIPT)
            total += update_control(conn, "image", "add", IMAGE_HTML, IMAGE_SCRIPT)
            total += update_control(conn, "image", "edit", IMAGE_HTML, IMAGE_SCRIPT)
            conn.commit()
        except Exception:
            conn.rollback()
            raise

    print(f"updated upload controls: {total}")
    print(f"done: {db_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
