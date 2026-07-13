import sqlite3
import sys

db_path = sys.argv[1] if len(sys.argv) > 1 else 'CodeMaster.Migrator/CodeMaster_Test.db'
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# 查询所有菜单
cursor.execute("SELECT menu_name, component, path, menu_type FROM sys_menu WHERE component LIKE '%codegen%' OR path LIKE '%codegen%' OR menu_name LIKE '%代码管理%'")
rows = cursor.fetchall()

if rows:
    print(f"找到 {len(rows)} 个相关菜单:")
    for row in rows:
        print(f"  {row[0]} | {row[1]} | {row[2]} | {row[3]}")
else:
    print("没有找到 codegen 相关菜单")

conn.close()
