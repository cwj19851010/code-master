# API 方法更新总结报告

## 已完成更新的模块

### ✅ 1. system/post (完成)
- **index.vue**: `getPostList` → `getPagedList`, `deletePost` → `deleteById`
- **add.vue**: `createPost` → `create`
- **edit.vue**: `getPostById` → `getById`, `updatePost` → `update`
- **detail.vue**: `getPostById` → `getById`

### ✅ 2. system/dept (完成)
- **index.vue**: 使用 `getList`, `create`, `update`, `deleteById`

### ✅ 3. system/role (完成)
- **index.vue**: `getRoleList` → `getPagedList`, `deleteRole` → `deleteById`
- **add.vue**: `createRole` → `create`
- **edit.vue**: `getRoleById` → `getById`, `updateRole` → `update`
- **detail.vue**: `getRoleById` → `getById`

### ✅ 4. system/tenant (完成)
- **index.vue**: `getTenantList` → `getPagedList`, `deleteTenant` → `deleteById`
- **add.vue**: `createTenant` → `create`
- **edit.vue**: `getTenantById` → `getById`, `updateTenant` → `update`
- **detail.vue**: `getTenantById` → `getById`

### ✅ 5. system/menu (完成)
- **index.vue**: `getMenuList` → `getList`, `deleteMenu` → `deleteById`
- **add.vue**: `getMenuList` → `getList`, `createMenu` → `create`
- **edit.vue**: `getMenuList` → `getList`, `getMenuById` → `getById`, `updateMenu` → `update`
- **detail.vue**: `getMenuById` → `getById`

### ✅ 6. system/lang (完成)
- **index.vue**:
  - Lang: `getLangList` → `getPagedList`, `createLang` → `create`, `updateLang` → `update`, `deleteLang` → `deleteById`
  - Text: `getLangTextList` → `getTextPagedList`, `createLangText` → `createText`, `updateLangText` → `updateText`, `deleteLangText` → `deleteTextById`

## 需要更新的模块

### 🔄 7. system/dict (待更新)
需要更新的文件：
- `index.vue`
- `type/add.vue`, `type/edit.vue`, `type/detail.vue`
- `data/add.vue`, `data/edit.vue`, `data/detail.vue`

API 方法映射：
- Type: `getTypePagedList`, `getTypeById`, `createType`, `updateType`, `deleteTypeById`
- Data: `getDataPagedList`, `getDataById`, `createData`, `updateData`, `deleteDataById`

### 🔄 8. monitor/task (待更新)
需要更新的文件：
- `index.vue`, `add.vue`, `edit.vue`, `detail.vue`

API 方法映射：
- `getPagedList`, `getById`, `create`, `update`, `deleteById`

### 🔄 9. monitor/tasklog (待更新)
需要更新的文件：
- `index.vue`, `detail.vue`

API 方法映射：
- `getPagedList`, `getById`

### 🔄 10. monitor/loginlog (待更新)
需要更新的文件：
- `index.vue`, `detail.vue`

需要检查 API 文件是否存在

### 🔄 11. monitor/operlog (待更新)
需要更新的文件：
- `index.vue`, `detail.vue`

需要检查 API 文件是否存在

### 🔄 12. project/project (待更新)
需要更新的文件：
- `index.vue`, `add.vue`, `edit.vue`, `detail.vue`

API 方法映射：
- `getPagedList`, `getById`, `create`, `update`, `deleteById`

### 🔄 13. project/projectModule (待更新)
需要更新的文件：
- `index.vue`, `add.vue`, `edit.vue`, `detail.vue`

API 方法映射：
- `getPagedList`, `getById`, `create`, `update`, `deleteById`

### 🔄 14. project/moduleEntity (待更新)
需要更新的文件：
- `index.vue`, `add.vue`, `edit.vue`, `detail.vue`

API 方法映射：
- `getPagedList`, `getById`, `create`, `update`, `deleteById`

### 🔄 15. project/entityField (待更新)
需要更新的文件：
- `index.vue`

API 方法映射：
- `getPagedList`, `getById`, `create`, `update`, `deleteById`

## 更新规则总结

### Import 语句更新
```javascript
// 旧的导入方式（各种命名）
import { getUserList, deleteUser } from '@/api/user'
import { getPostList, deletePost } from '@/api/post'

// 新的导入方式（统一命名）
import { getPagedList, deleteById } from '@/api/user'
import { getPagedList, deleteById } from '@/api/post'
```

### 方法调用更新
```javascript
// 列表查询
request.get('/xxx/getpagedlist') → getPagedList(params)
request.get('/xxx/getlist') → getList(params)

// 详情查询
request.get(`/xxx/getbyid/${id}`) → getById(id)

// 创建
request.post('/xxx/create', data) → create(data)

// 更新
request.put(`/xxx/update/${id}`, data) → update(id, data)

// 删除
request.delete(`/xxx/delete/${id}`) → deleteById(id)
```

## 进度统计

- ✅ 已完成: 6 个模块
- 🔄 待完成: 9 个模块
- 📊 完成率: 40%

## 下一步操作

建议按以下顺序继续更新：
1. system/dict (7个文件)
2. monitor/task (4个文件)
3. monitor/tasklog (2个文件)
4. monitor/loginlog (2个文件)
5. monitor/operlog (2个文件)
6. project/project (4个文件)
7. project/projectModule (4个文件)
8. project/moduleEntity (4个文件)
9. project/entityField (1个文件)

总计约 30 个文件需要更新。
