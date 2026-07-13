# 页面缓存实现说明

## 概述
实现了基于后台菜单配置的动态页面缓存功能，通过 `IsCache` 字段控制页面是否缓存。

## 实现细节

### 1. 后端修改

#### SysMenuService.cs
- 修复了 `NoCache` 字段的逻辑：`NoCache = menu.IsCache == 0`
  - `IsCache = 1` 表示缓存页面，`NoCache = false`
  - `IsCache = 0` 表示不缓存，`NoCache = true`
- 添加了 `GenerateComponentName` 方法，根据组件路径生成组件名称
  - 例如：`system/user/index` → `SystemUser`
  - 例如：`system/user/add` → `SystemUserAdd`

### 2. 前端修改

#### 组件名称定义
为所有列表页面添加了 `defineOptions` 定义组件名称：

**System 模块：**
- SystemUser
- SystemDept
- SystemRole
- SystemPost
- SystemMenu
- SystemDict
- SystemTenant
- SystemFile
- SystemLang

**Monitor 模块：**
- MonitorLoginlog
- MonitorOperlog
- MonitorTask
- MonitorTasklog
- MonitorOnline

**CodeGen 模块：**
- CodegenProject
- CodegenProjectModule
- CodegenModuleEntity
- CodegenEntityField

#### Layout 组件
修改了 `keep-alive` 配置：
```vue
<keep-alive :include="cachedViews" :max="20">
  <component :is="Component" :key="route.path" />
</keep-alive>
```

#### TagsView Store
已有的逻辑会根据路由的 `meta.noCache` 自动管理缓存列表：
- `addCachedView`: 如果 `meta.noCache = true`，不添加到缓存
- `delCachedView`: 关闭标签时移除缓存

## 工作流程

1. 后端返回路由时，根据菜单的 `IsCache` 字段设置 `meta.noCache`
2. 后端根据组件路径生成标准化的组件名称（如 `SystemUser`）
3. 前端组件通过 `defineOptions` 定义相同的组件名称
4. 用户访问页面时，TagsView store 检查 `meta.noCache`：
   - 如果 `noCache = false`（需要缓存），将组件名称添加到 `cachedViews` 数组
   - 如果 `noCache = true`（不缓存），不添加
5. Layout 组件的 `keep-alive` 使用 `include` 属性，只缓存 `cachedViews` 中的组件

## 测试方法

1. 登录系统
2. 访问用户列表页面，进行筛选或翻页
3. 点击"新增"或"编辑"跳转到其他页面
4. 点击浏览器返回按钮或标签页返回
5. 验证列表页面的筛选条件和分页状态是否保留

## 配置方式

在菜单管理中，可以为每个菜单配置 `IsCache` 字段：
- `IsCache = 1`：缓存该页面（默认）
- `IsCache = 0`：不缓存该页面

## 注意事项

1. 组件名称必须与后端生成的名称一致
2. 只有列表页面（index.vue）需要定义组件名称
3. 详情、新增、编辑页面通常不需要缓存
4. 缓存数量限制为 20 个页面（可通过 `:max` 属性调整）
