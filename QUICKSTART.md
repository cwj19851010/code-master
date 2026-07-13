# CodeMaster 快速启动指南

## 项目简介

CodeMaster 是一个基于 .NET 8 + Vue 3 的企业级后台管理系统，采用前后端分离架构。

### 技术栈

**后端：**
- .NET 8.0
- EF Core 8.0 + SqlSugar（双ORM架构）
- JWT 认证
- Swagger API 文档
- 雪花ID算法

**前端：**
- Vue 3
- Element Plus
- Axios
- Vue Router

## 快速开始

### 1. 环境要求

- .NET 8.0 SDK
- Node.js 16+
- SQL Server 2019+

### 2. 数据库配置

修改 `CodeMaster.WebApi/appsettings.Development.json` 中的数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CodeMaster;User Id=sa;Password=your_password;TrustServerCertificate=True;"
  }
}
```

### 3. 数据库迁移

```bash
# 进入迁移工具目录
cd CodeMaster.Migrator

# 运行迁移工具（会自动创建数据库表和初始化种子数据）
dotnet run
```

**默认账号：**
- 用户名：admin
- 密码：admin123

### 4. 启动后端

```bash
# 进入 WebApi 目录
cd CodeMaster.WebApi

# 启动后端服务
dotnet run
```

后端服务将在 `https://localhost:5001` 启动，Swagger 文档地址：`https://localhost:5001/swagger`

### 5. 启动前端

```bash
# 进入前端目录
cd CodeMaster.Vue

# 安装依赖（首次运行）
npm install

# 启动开发服务器
npm run dev
```

前端服务将在 `http://localhost:5173` 启动

## 项目结构

```
CodeMaster/
├── CodeMaster.Core/              # 核心层（实体基类、仓储接口、服务接口）
├── CodeMaster.Domain/            # 领域层（实体定义）
├── CodeMaster.Application/       # 应用层（业务逻辑、DTO）
├── CodeMaster.Infrastructure/    # 基础设施层（数据访问、EF Core、SqlSugar）
├── CodeMaster.WebApi/            # Web API 层（控制器、中间件）
├── CodeMaster.Migrator/          # 数据库迁移工具
└── CodeMaster.Vue/               # 前端项目
```

## 功能模块

### 已实现功能

- ✅ JWT 认证授权
- ✅ 用户管理（CRUD）
- ✅ 角色管理（CRUD）
- ✅ 部门管理（CRUD）
- ✅ 菜单管理（CRUD）
- ✅ 租户管理（多租户支持）
- ✅ 统一异常处理
- ✅ 统一响应格式
- ✅ Swagger API 文档

### 核心特性

1. **双ORM架构**：支持 EF Core 和 SqlSugar 快速切换
2. **多租户支持**：逻辑隔离 + 物理隔离
3. **雪花ID**：分布式唯一ID生成
4. **统一列名**：数据库列名采用 snake_case 格式
5. **JWT认证**：基于 Token 的身份验证

## API 文档

启动后端后，访问 Swagger 文档：`https://localhost:5001/swagger`

### 主要接口

- **认证接口**：`/api/auth/login`、`/api/auth/userinfo`
- **用户管理**：`/api/system/user`
- **角色管理**：`/api/system/role`
- **部门管理**：`/api/system/dept`
- **菜单管理**：`/api/system/menu`
- **租户管理**：`/api/system/tenant`

## 开发说明

### 添加新实体

1. 在 `CodeMaster.Domain/Entities` 中创建实体类
2. 在 `CodeMaster.Infrastructure/Persistence/EfCore` 中添加 EF Core 配置
3. 在 `CodeMaster.Application` 中创建 DTO 和 Service
4. 在 `CodeMaster.WebApi/Controllers` 中创建 Controller

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName --project CodeMaster.Infrastructure --startup-project CodeMaster.WebApi

# 更新数据库
dotnet ef database update --project CodeMaster.Infrastructure --startup-project CodeMaster.WebApi
```

## 常见问题

### 1. 数据库连接失败

检查 SQL Server 是否启动，连接字符串是否正确。

### 2. 前端跨域问题

后端已配置 CORS，允许所有来源访问（开发环境）。

### 3. JWT Token 过期

默认过期时间为 7 天，可在 `appsettings.json` 中修改。

## 联系方式

如���问题，请提交 Issue。
