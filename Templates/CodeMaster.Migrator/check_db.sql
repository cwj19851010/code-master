SELECT '=== 菜单统计 ===' as info;
SELECT COUNT(*) as total_menus FROM sys_menu;
SELECT MenuType, COUNT(*) as count FROM sys_menu GROUP BY MenuType;

SELECT '=== 翻译统计 ===' as info;
SELECT COUNT(*) as total_translations FROM sys_lang_text;
SELECT LangCode, COUNT(*) as count FROM sys_lang_text GROUP BY LangCode;

SELECT '=== 用户统计 ===' as info;
SELECT COUNT(*) as total_users FROM sys_user;

SELECT '=== 角色统计 ===' as info;
SELECT COUNT(*) as total_roles FROM sys_role;

SELECT '=== 菜单列表（顶级） ===' as info;
SELECT Id, MenuName, TitleKey, Path, Component, MenuType FROM sys_menu WHERE ParentId = 0 ORDER BY OrderNum;
