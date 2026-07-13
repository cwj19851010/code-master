-- 更新菜单的 ancestors 字段
-- 使用递归 CTE 计算每个菜单的祖先路径

WITH MenuHierarchy AS (
    -- 基础情况：根节点（parent_id IS NULL 或 parent_id = 0）
    SELECT
        id,
        parent_id,
        menu_name,
        '0' as ancestors,
        0 as level
    FROM sys_menus
    WHERE parent_id IS NULL OR parent_id = 0

    UNION ALL

    -- 递归情况：子节点
    SELECT
        m.id,
        m.parent_id,
        m.menu_name,
        CASE
            WHEN mh.ancestors = '0' THEN CONCAT('0,', CAST(mh.id AS VARCHAR(20)))
            ELSE CONCAT(mh.ancestors, ',', CAST(mh.id AS VARCHAR(20)))
        END as ancestors,
        mh.level + 1 as level
    FROM sys_menus m
    INNER JOIN MenuHierarchy mh ON m.parent_id = mh.id
)
UPDATE m
SET ancestors = mh.ancestors
FROM sys_menus m
INNER JOIN MenuHierarchy mh ON m.id = mh.id;

-- 验证更新结果
SELECT id, menu_name, parent_id, ancestors,
       LEN(ancestors) - LEN(REPLACE(ancestors, ',', '')) + 1 as depth
FROM sys_menus
ORDER BY ancestors, id;
