# 数据库配置说明

CodeMaster 支持多种数据库，通过条件编译实现。

## 支持的数据库

1. **SQL Server** (默认)
2. **MySQL**
3. **PostgreSQL**
4. **SQLite**
5. **Oracle**

## 配置方法

### 1. SQL Server (默认)

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CodeMasterDb;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "DbProvider": "SqlServer"
}
```

**构建和运行:**
```bash
cd CodeMaster.Migrator
dotnet build
dotnet run
```

### 2. MySQL

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CodeMasterDb;User=root;Password=yourpassword;"
  },
  "DbProvider": "MySql"
}
```

**构建和运行:**
```bash
cd CodeMaster.Migrator
dotnet build -p:DbProvider=MySql
dotnet run
```

### 3. PostgreSQL

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=CodeMasterDb;Username=postgres;Password=yourpassword"
  },
  "DbProvider": "PostgreSQL"
}
```

**构建和运行:**
```bash
cd CodeMaster.Migrator
dotnet build -p:DbProvider=PostgreSQL
dotnet run
```

### 4. SQLite

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=CodeMaster.db"
  },
  "DbProvider": "Sqlite"
}
```

**构建和运行:**
```bash
cd CodeMaster.Migrator
dotnet build -p:DbProvider=Sqlite
dotnet run
```

### 5. Oracle

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "User Id=system;Password=oracle;Data Source=localhost:1521/ORCL"
  },
  "DbProvider": "Oracle"
}
```

**构建和运行:**
```bash
cd CodeMaster.Migrator
dotnet build -p:DbProvider=Oracle
dotnet run
```

## EF Core 迁移命令

### 添加迁移

```bash
# SQL Server
dotnet ef migrations add MigrationName

# MySQL
dotnet ef migrations add MigrationName -p:DbProvider=MySql

# PostgreSQL
dotnet ef migrations add MigrationName -p:DbProvider=PostgreSQL

# SQLite
dotnet ef migrations add MigrationName -p:DbProvider=Sqlite

# Oracle
dotnet ef migrations add MigrationName -p:DbProvider=Oracle
```

### 更新数据库

```bash
# SQL Server
dotnet ef database update

# MySQL
dotnet ef database update -p:DbProvider=MySql

# PostgreSQL
dotnet ef database update -p:DbProvider=PostgreSQL

# SQLite
dotnet ef database update -p:DbProvider=Sqlite

# Oracle
dotnet ef database update -p:DbProvider=Oracle
```

## 注意事项

1. **SQLite** 适合开发和测试，不建议用于生产环境
2. **Oracle** 需要安装 Oracle Client
3. 切换数据库后需要重新构建项目
4. 不同数据库的SQL语法可能有差异，迁移时需要注意
