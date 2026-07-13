# CodeMaster 快速开始指南

## 项目简介

CodeMaster（代码大师）是一个现代化的企业级快速开发平台，采用前后端分离架构。

- **后端**: .NET 8.0 + EF Core + SqlSugar
- **前端**: Vue 3 + Vite + Element Plus
- **数据库**: SQL Server

## 环境要求

### 后端
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 或 VS Code

### 前端
- Node.js 18+
- npm 或 yarn

## 快速启动

### 1. 配置数据库

修改以下文件中的数据库连接字符串：

- `CodeMaster.WebApi/appsettings.Development.json`
- `CodeMaster.Migrator/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CodeMasterDb;User Id=sa;Password=YourPassword123;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### 2. 运行数据库迁移

```bash
cd CodeMaster.Migrator
dotnet run
```

这将自动：
- 创建数据库和表结构
- 初始化种子数据
- 创建默认管理员账号：`admin` / `admin123`

### 3. 启动后端 API

```bash
cd CodeMaster.WebApi
dotnet run
```

API 服务将在 https://localhost:5001 启动

访问 Swagger 文档：https://localhost:5001/swagger

### 4. 安装前端依赖

```bash
cd CodeMaster.Vue
npm install
```

### 5. 启动前端开发服务器

```bash
npm run dev
```

前端将在 http://localhost:3000 启动

### 6. 登录系统

打开浏览器访问 http://localhost:3000

使用默认账号登录：
- 用户名：`admin`
- 密码：`admin123`

## 项目结构

```
CodeMaster/
├── CodeMaster.Core/              # 核心层 - 基础接口
├── CodeMaster.Domain/            # 领域层 - 实体定义
├── CodeMaster.Infrastructure/    # 基础设施层 - 数据访问
├── CodeMaster.Application/       # 应用层 - 业务逻辑
├── CodeMaster.WebApi/            # Web API 层
├── CodeMaster.Migrator/          # 数据库迁移工具
└── CodeMaster.Vue/               # Vue 前端项目
```

## 核心功能

### 已实现
- ✅ DDD 分层架构
- ✅ EF Core + SqlSugar 双 ORM
- ✅ 用户管理 CRUD
- ✅ 角色管理 CRUD
- ✅ 部门管理 CRUD
- ✅ 菜单管理 CRUD
- ✅ Swagger API 文档
- ✅ 雪花 ID 生成
- ✅ BCrypt 密码加密
- ✅ Vue 3 前端框架
- ✅ Element Plus UI

### 待实现
- ⏳ JWT 认证授权
- ⏳ 多租户支持
- ⏳ 代码生成器
- ⏳ 操作日志
- ⏳ 数据权限
- ⏳ 文件上传

## API 接口

### 用户管理
- `GET /api/system/user/{id}` - 获取用户详情
- `GET /api/system/user/list` - 获取用户列表
- `POST /api/system/user` - 创建用户
- `PUT /api/system/user` - 更新用户
- `DELETE /api/system/user/{id}` - 删除用户

### 角色管理
- `GET /api/system/role/{id}` - 获取角色详情
- `GET /api/system/role/list` - 获取角色列表
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

## 常见问题

### 1. 数据库连接失败

检查：
- SQL Server 是否启动
- 连接字符串是否正确
- 用户名密码是否正确
- 防火墙是否允许连接

### 2. 前端无法访问后端 API

检查：
- 后端 API 是否启动
- CORS 配置是否正确
- 代理配置是否正确（vite.config.js）

### 3. 编译错误

尝试：
```bash
# 清理并重新构建
dotnet clean
dotnet build
```

## 开发建议

1. 使用 Visual Studio 2022 或 VS Code 开发后端
2. 使用 VS Code 开发前端
3. 安装推荐的 VS Code 扩展：
   - Vue Language Features (Volar)
   - ESLint
   - Prettier

## 技术支持

- GitHub: https://github.com/codemaster/codemaster
- Email: support@codemaster.com

## 许可证

MIT License
