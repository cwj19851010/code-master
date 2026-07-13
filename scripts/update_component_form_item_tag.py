#!/usr/bin/env python3
import json
import sqlite3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MIGRATOR_DIR = ROOT / "CodeMaster.Migrator"
APPSETTINGS = MIGRATOR_DIR / "appsettings.json"

COMPONENT_FIXES = {
    "form-item": ("form-item", "form-item"),
    "button-group": ("button-group", "button-group"),
    "checkbox-group": ("checkbox-group", "checkbox-group"),
    "checkbox-button": ("checkbox-button", "checkbox-button"),
    "radio-group": ("radio-group", "radio-group"),
    "radio-button": ("radio-button", "radio-button"),
    "option -group": ("option-group", "option-group"),
    "option-group": ("option-group", "option-group"),
    "header": ("header", "header"),
    "main": ("main", "main"),
    "footer": ("footer", "footer"),
    "aside": ("aside", "aside"),
    "row": ("row", "row"),
    "col": ("col", "col"),
    "sub-menu": ("sub-menu", "sub-menu"),
    "menu--item": ("menu-item", "menu-item"),
    "menu-item": ("menu-item", "menu-item"),
    "menu--item--group": ("menu-item-group", "menu-item-group"),
    "menu-item-group": ("menu-item-group", "menu-item-group"),
    "breadcrumb-item": ("breadcrumb-item", "breadcrumb-item"),
    "collapse -item": ("collapse-item", "collapse-item"),
    "collapse-item": ("collapse-item", "collapse-item"),
    "carousel--item": ("carousel-item", "carousel-item"),
    "carousel-item": ("carousel-item", "carousel-item"),
    "tab-pane": ("tab-pane", "tab-pane"),
    "dropdown--menu": ("dropdown-menu", "dropdown-menu"),
    "dropdown-menu": ("dropdown-menu", "dropdown-menu"),
    "dropdown--item": ("dropdown-item", "dropdown-item"),
    "dropdown-item": ("dropdown-item", "dropdown-item"),
    "descriptions-item": ("descriptions-item", "descriptions-item"),
    "step": ("step", "step"),
    "timeline--item": ("timeline-item", "timeline-item"),
    "timeline-item": ("timeline-item", "timeline-item"),
    "skeleton-item": ("skeleton-item", "skeleton-item"),
    "table-column": ("table-column", "table-column"),
    "option": ("option", "option"),
    "cascader-panel": ("cascader-panel", "cascader-panel"),
    "check-tag": ("check-tag", "check-tag"),
    "countdown": ("countdown", "countdown"),
    "tab-nav": ("tab-nav", "tab-nav"),
    "splitter-panel": ("splitter-panel", "splitter-panel"),
}


def resolve_sqlite_path() -> Path:
    config = json.loads(APPSETTINGS.read_text(encoding="utf-8-sig"))
    connection = config["ConnectionStrings"]["DefaultConnection"]
    prefix = "Data Source="
    if not connection.startswith(prefix):
        raise RuntimeError(f"Only sqlite Data Source connections are supported: {connection}")

    db_path = Path(connection[len(prefix):])
    if not db_path.is_absolute():
        db_path = MIGRATOR_DIR / db_path
    return db_path


def main() -> None:
    db_path = resolve_sqlite_path()
    if not db_path.exists():
        raise FileNotFoundError(db_path)

    with sqlite3.connect(db_path) as conn:
        rows_before = conn.execute(
            "select id, name, tag from sys_components order by name, id"
        ).fetchall()

        updated = 0
        for current_name, (target_name, target_tag) in COMPONENT_FIXES.items():
            updated += conn.execute(
                "update sys_components set name = ?, tag = ?, update_time = datetime('now') "
                "where name = ? and (name <> ? or tag <> ?)",
                (target_name, target_tag, current_name, target_name, target_tag),
            ).rowcount
        conn.commit()

        rows_after = conn.execute(
            "select id, name, tag from sys_components order by name, id"
        ).fetchall()

    print(f"Database: {db_path}")
    print(f"Rows updated: {updated}")
    changed_names = set(COMPONENT_FIXES) | {target[0] for target in COMPONENT_FIXES.values()}
    print("Before:")
    for row in rows_before:
        if row[1] in changed_names or row[2] in changed_names:
            print(f"  id={row[0]} name={row[1]} tag={row[2]}")
    print("After:")
    for row in rows_after:
        if row[1] in changed_names or row[2] in changed_names:
            print(f"  id={row[0]} name={row[1]} tag={row[2]}")

    duplicate_tags = {}
    for _, name, tag in rows_after:
        duplicate_tags.setdefault(tag, []).append(name)
    duplicates = {tag: names for tag, names in duplicate_tags.items() if len(names) > 1}
    if duplicates:
        print("Duplicate tags still present:")
        for tag, names in sorted(duplicates.items()):
            print(f"  {tag}: {', '.join(sorted(names))}")
    else:
        print("Duplicate tags still present: none")


if __name__ == "__main__":
    main()
