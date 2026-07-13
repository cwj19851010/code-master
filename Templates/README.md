# CodeMaster（代码大师）

企业级快速开发平台 - 基于 .NET 8.0 + Vue 3 + EF Core + SqlSugar

## 项目简介

CodeMaster 是一个现代化的企业级快速开发平台，采用前后端分离架构和 DDD 分层设计，集成了 EF Core 和 SqlSugar 双 ORM 方案，提供完整的 RBAC 权限管理、多租户支持和 JWT 认证功能。

## 技术栈

### 后端
- **.NET 8.0** - 最新的 .NET 框架
- **EF Core 8.0** - 主要 ORM，用于数据库迁移和复杂查询
- **SqlSugar** - 辅助 ORM，用于高性能 CRUD 操作
- **SQL Server** - 数据库
- **JWT** - 身份认证
- **Swagger** - API 文档
- **Yitter.IdGenerator** - 雪花 ID 生成器
- **BCrypt.Net** - 密码加密

### 前端
- **Vue 3** - 渐进式 JavaScript 框架
- **Element Plus** - Vue 3 UI 组件库
- **Axios** - HTTP 客户端
- **Vue Router** - 路由管理

## 项目结构

```
CodeMaster/
├── CodeMaster.Core/              # 核心层 - 基础接口和抽象
│   ├── Entities/                 # 实体基类
│   ├── Repositories/             # 仓储接口
│   ├── Services/                 # 服务接口
│   ├── Dtos/                     # DTO 基类
│   └── MultiTenancy/             # 多租户接口
├── CodeMaster.Domain/            # 领域层 - 实体定义
│   └── Entities/
│       └── System/               # 系统模块实体
├── CodeMaster.Infrastructure/    # 基础设施层 - 数据访问
│   ├── Persistence/
│   │   ├── EfCore/               # EF Core 配置
│   │   ├── SqlSugar/             # SqlSugar 配置
│   │   └── Repositories/         # 仓储实现
│   └── MultiTenancy/             # 多租户实现
├── CodeMaster.Application/       # 应用层 - 业务逻辑
│   ├── Services/                 # 服务实现
│   │   ├── System/               # 系统服务
│   │   └── Auth/                 # 认证服务
│   └── Dtos/                     # 数据传输对象
├── CodeMaster.WebApi/            # Web API 层
│   └── Controllers/              # API 控制器
├── CodeMaster.Migrator/          # 数据库迁移工具
└── CodeMaster.Vue/               # 前端项目
    ├── src/
    │   ├── api/                  # API 接口
    │   ├── views/                # 页面组件
    │   ├── router/               # 路由配置
    │   ├── utils/                # 工具函数
    │   └── layout/               # 布局组件
    └── package.json
```

## 快速开始

### 方式一：使用启动脚本（推荐）

**Windows:**
```bash
start.bat
```

**Linux/Mac:**
```bash
chmod +x start.sh
./start.sh
```

### 方式二：手动启动

详见 [QUICKSTART.md](QUICKSTART.md)

### 默认账号

- 用户名：`admin`
- 密码：`admin123`

## 核心功能

### 已实现功能

#### 后端
- ✅ DDD 分层架构
- ✅ EF Core + SqlSugar 双 ORM
- ✅ JWT 认证授权
- ✅ 多租户支持（逻辑隔离 + 物理隔离）
- ✅ 雪花 ID 生成
- ✅ 数据库迁移和种子数据
- ✅ Swagger API 文档
- ✅ 统一异常处理
- ✅ 统一响应格式
- ✅ 用户管理 CRUD
- ✅ 角色管理 CRUD
- ✅ 部门管理 CRUD
- ✅ 菜单管理 CRUD
- ✅ 租户管理 CRUD
- ✅ 密码 BCrypt 加密
- ✅ Snake_case 数据库列名

#### 前端
- ✅ Vue 3 + Element Plus
- ✅ 登录认证
- ✅ 路由守卫
- ✅ Token 拦截器
- ✅ 用户管理页面
- ✅ 角色管理页面
- ✅ 部门管理页面
- ✅ 菜单管理页面
- ✅ 租户管理页面
- ✅ 统一 API 封装

### 待实现功能

- ⏳ 代码生成器
- ⏳ 操作日志
- ⏳ 数据权限
- ⏳ 文件上传
- ⏳ 在线用户管理
- ⏳ 系统监控

## API 接口

### 认证接口

- `POST /api/auth/login` - 用户登录
- `GET /api/auth/userinfo` - 获取当前用户信息
- `POST /api/auth/logout` - 退出登录

### 用户管理

- `GET /api/system/user/{id}` - 获取用户详情
- `GET /api/system/user/list` - 获取用户分页列表
- `POST /api/system/user` - 创建用户
- `PUT /api/system/user` - 更新用户
- `DELETE /api/system/user/{id}` - 删除用户

### 角色管理

- `GET /api/system/role/{id}` - 获取角色详情
- `GET /api/system/role/list` - 获取角色分页列表
- `POST /api/system/role` - 创建角色
- `PUT /api/system/role` - 更新角色
- `DELETE /api/system/role/{id}` - 删除角色

### 部门管理

- `GET /api/system/dept/{id}` - 获取部门详情
- `GET /api/system/dept/list` - 获取部门列表
- `POST /api/system/dept` - 创建部门
- `PUT /api/system/dept` - 更新部门
- `DELETE /api/system/dept/{id}` - 删除部门

### 菜单管理

- `GET /api/system/menu/{id}` - 获取菜单详情
- `GET /api/system/menu/list` - 获取菜单列表
- `POST /api/system/menu` - 创建菜单
- `PUT /api/system/menu` - 更新菜单
- `DELETE /api/system/menu/{id}` - 删除菜单

### 租户管理

- `GET /api/system/tenant/{id}` - 获取租户详情
- `GET /api/system/tenant/list` - 获取租户分页列表
- `POST /api/system/tenant` - 创建租户
- `PUT /api/system/tenant` - 更新租户
- `DELETE /api/system/tenant/{id}` - 删除租户
- `POST /api/system/tenant/test-connection` - 测试数据库连接

## 数据库设计

### 核心表

- `sys_user` - 用户表
- `sys_role` - 角色表
- `sys_dept` - 部门表
- `sys_menu` - 菜单表
- `sys_tenant` - 租户表
- `sys_user_role` - 用户角色关联表
- `sys_role_menu` - 角色菜单关联表

所有表字段统一使用 `snake_case` 命名规范。

## 开发规范

### 实体命名

- 实体类继承 `EntityBase` 或 `TenantEntityBase`
- 使用 `[SugarTable]` 和 `[SugarColumn]` 特性标注

### DTO 命名

- 查询 DTO：`{Entity}Dto`
- 创建 DTO：`Create{Entity}Dto`
- 更新 DTO：`Update{Entity}Dto`
- 查询条件 DTO：`{Entity}QueryDto`

### 服务命名

- 接口：`I{Entity}Service`
- 实现：`{Entity}Service`

### 控制器命名

- 类名：`{Entity}Controller`
- 路由：`/api/{module}/{entity}`

## 多租户说明

系统支持两种租户隔离方式：

1. **逻辑隔离**：共享数据库，通过 TenantId 字段过滤数据
2. **物理隔离**：独立数据库，每个租户使用独立的连接字符串

租户识别方式：
- HTTP 请求头：`X-Tenant-Id`
- JWT Token Claims：`TenantId`

## 许可证

MIT License

## 联系方式

如有问题，请提交 Issue。
