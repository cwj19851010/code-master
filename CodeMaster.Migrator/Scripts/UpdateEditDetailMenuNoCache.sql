-- Make query/id-based edit and detail routes non-cacheable.
-- visible uses bool semantics: 1/TRUE = shown in menu, 0/FALSE = hidden route.
-- is_cache uses bool semantics: 1/TRUE = cached, 0/FALSE = no cache.

UPDATE sys_menus
SET is_cache = 0,
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
