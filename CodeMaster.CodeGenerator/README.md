# CodeMaster 代码生成器

## 简介

CodeMaster.CodeGenerator 是一个基于反射和模板引擎的代码生成工具，能够自动扫描 Domain 层的实体，并生成对应的 DTO、Service 和 Controller 代码。

## 功能特性

- ✅ 自动扫描 Domain 实体
- ✅ 生成标准的 CRUD DTO（QueryDto、Dto、CreateDto、UpdateDto）
- ✅ 生成 Service 接口和实现类
- ✅ 生成 RESTful API Controller
- ✅ 支持多租户实体识别
- ✅ 支持模块化命名空间
- ✅ 使用 Scriban 模板引擎，易于自定义

## 使用方法

### 1. 运行代码生成器

```bash
cd CodeMaster.CodeGenerator
dotnet run
```

### 2. 查看生成结果

生成的代码位于 `Generated` 目录：

```
Generated/
├── Controllers/
│   ├── System/
│   │   ├── SysUserController.cs
│   │   ├── SysRoleController.cs
│   │   └── ...
│   └── CodeGen/
│       ├── GenTableController.cs
│       └── ...
├── Dtos/
│   ├── System/
│   │   ├── SysUserDto.cs
│   │   ├── SysRoleDto.cs
│   │   └── ...
│   └── CodeGen/
│       └── ...
└── Services/
    ├── System/
    │   ├── ISysUserService.cs
    │   ├── SysUserService.cs
    │   └── ...
    └── CodeGen/
        └── ...
```

### 3. 复制代码到项目

根据需要将生成的代码复制到对应的项目中：

- `Dtos/` → `CodeMaster.Application/Dtos/`
- `Services/` → `CodeMaster.Application/Services/`
- `Controllers/` → `CodeMaster.WebApi/Controllers/`

## 生成的代码结构

### DTO

每个实体会生成 4 个 DTO：

1. **QueryDto**: 用于查询条件，所有字段都是可空的
2. **Dto**: 用于返回数据，包含所有字段
3. **CreateDto**: 用于创建实体，排除 Id、CreateTime、UpdateTime 等字段
4. **UpdateDto**: 用于更新实体，排除 Id、CreateTime、UpdateTime 等字段

### Service

每个实体会生成：

1. **IService 接口**: 定义标准 CRUD 方法
2. **Service 实现**: 使用 SqlSugar 实现 CRUD 操作

标准方法：
- `GetByIdAsync(long id)`: 根据 ID 获取单个实体
- `GetPagedListAsync(QueryDto query)`: 分页查询
- `CreateAsync(CreateDto dto)`: 创建实体
- `UpdateAsync(long id, UpdateDto dto)`: 更新实体
- `DeleteAsync(long id)`: 删除实体
- `BatchDeleteAsync(long[] ids)`: 批量删除

### Controller

每个实体会生成一个 RESTful API Controller：

- `GET /api/{entity}/page`: 分页查询
- `GET /api/{entity}/{id}`: 获取详情
- `POST /api/{entity}`: 创建
- `PUT /api/{entity}/{id}`: 更新
- `DELETE /api/{entity}/{id}`: 删除
- `DELETE /api/{entity}/batch`: 批量删除

## 自定义模板

模板文件位于 `Templates/` 目录：

- `DtoTemplate.txt`: DTO 生成模板
- `ServiceInterfaceTemplate.txt`: Service 接口模板
- `ServiceTemplate.txt`: Service 实现模板
- `ControllerTemplate.txt`: Controller 模板

模板使用 Scriban 语法，可以根据需要自定义。

### 模板变量

- `entity_name`: 实体名称（如 SysUser）
- `module_name`: 模块名称（如 System）
- `description`: 实体描述
- `properties`: 属性列表
  - `name`: 属性名称
  - `type`: 属性类型
  - `is_nullable`: 是否可空
  - `description`: 属性描述

## 注意事项

1. 生成代码前请先编译 Domain 项目
2. 生成的代码仅作为基础模板，可能需要根据业务需求调整
3. 建议先检查生成的代码，确认无误后再复制到项目中
4. 如果实体有特殊业务逻辑，需要手动添加

## 扩展功能

### 添加新的代码生成类型

1. 在 `Templates/` 目录创建新模板
2. 在 `Generators/CodeGenerator.cs` 添加生成方法
3. 在 `Program.cs` 调用新的生成方法

### 自定义实体扫描规则

修改 `Scanners/EntityScanner.cs` 中的扫描逻辑。

## 技术栈

- .NET 8.0
- Scriban 5.9.1（模板引擎）
- Microsoft.CodeAnalysis.CSharp 4.8.0（代码分析）
