-- One-time migration from the legacy menu flag convention to bool semantics.
-- Legacy visible: 0 = shown, 1 = hidden.
-- New visible: 1/TRUE = shown, 0/FALSE = hidden.
-- is_cache already used 1 = cached, 0 = no cache, so it is not inverted.

UPDATE sys_menus
SET visible = CASE WHEN visible = 0 THEN TRUE ELSE FALSE END,
    update_time = NOW()
WHERE is_deleted = 0;

UPDATE sys_menus
SET is_cache = TRUE,
    update_time = NOW()
WHERE is_deleted = 0
  AND visible = TRUE
  AND (
    menu_type IN ('M', 'F')
    OR (
      menu_type = 'C'
      AND path NOT IN ('edit', 'detail', 'type/edit', 'type/detail', 'data/edit', 'data/detail', 'text/edit', 'text/detail')
      AND (component IS NULL OR (component NOT LIKE '%/edit' AND component NOT LIKE '%/detail'))
    )
  );

UPDATE sys_menus
SET is_cache = TRUE,
    update_time = NOW()
WHERE is_deleted = 0
  AND menu_type = 'C'
  AND (
    path = 'add'
    OR path LIKE '%/add'
    OR component LIKE '%/add'
  );

UPDATE sys_menus
SET is_cache = FALSE,
    update_time = NOW()
WHERE menu_type = 'C'
  AND is_deleted = 0
  AND (
    path = 'edit'
    OR path = 'detail'
    OR path LIKE '%/edit'
    OR path LIKE '%/detail'
    OR component LIKE '%/edit'
    OR component LIKE '%/detail'
  );

ALTER TABLE sys_menus MODIFY visible TINYINT(1) NOT NULL;
ALTER TABLE sys_menus MODIFY is_cache TINYINT(1) NOT NULL;
