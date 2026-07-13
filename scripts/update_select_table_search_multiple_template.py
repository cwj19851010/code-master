import shutil
import sqlite3
from datetime import datetime
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "CodeMaster.Migrator" / "CodeMaster_Test.db"


def main() -> None:
    if not DB_PATH.exists():
        raise FileNotFoundError(DB_PATH)

    backup = DB_PATH.with_name(f"{DB_PATH.name}.bak_{datetime.now():%Y%m%d%H%M%S}")
    shutil.copy2(DB_PATH, backup)
    print(f"backup: {backup}")

    html = (
        '<el-form-item :label="$t(\'[field.labelKey]\')" data-gen-id="gen_field_[field.id]">'
        '<el-select v-model="[field.formPrefix].[field.nameLower]" clearable [field.multipleAttr] '
        ':placeholder="$t(\'[field.labelKey]\')">'
        '<el-option v-for="item in [field.relatedEntityNameLower]Options" :key="item.value" '
        ':label="[field.relatedDisplayLabelExpr]" :value="item.value" />'
        '</el-select></el-form-item>'
    )

    with sqlite3.connect(DB_PATH) as conn:
        cur = conn.execute(
            """
            UPDATE sys_field_control_templates
            SET html_content = ?
            WHERE control_type = 'select-table'
              AND page_section = 'search'
              AND is_deleted = 0
            """,
            (html,),
        )
        conn.commit()
        print(f"updated select-table/search templates: {cur.rowcount}")


if __name__ == "__main__":
    main()
