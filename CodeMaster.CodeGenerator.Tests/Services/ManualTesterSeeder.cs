using CodeMaster.Domain.Entities.CodeGen;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

/// <summary>
/// 专供开发人员一键填充本地测试数据的工具测试
/// 运行此测试将强行连接你本地 WebApi/Migrator 所指的 SQLite 库注入大量可用演示数据
/// </summary>
public class ManualTesterSeeder
{
    [Fact(Skip = "如果不希望在正常管线运行请保持 Skip，若需在本地发种（Seed数据）请在VS中取消 Skip 并运行该测试")]
    // 若要在你本地注入数据，请把上面的 (Skip = "...") 删掉，然后运行该测试！
    public void SeedLocalData_RunMe()
    {
        // 定位到你本地真实的开发数据库位置
        var devDbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CodeMaster.Migrator", "CodeMaster_Test.db"));
        
        if (!File.Exists(devDbPath))
        {
            throw new Exception($"未能找到你的本地开发数据库文件: {devDbPath}");
        }

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"Data Source={devDbPath}",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });

        // 1. 注入一个虚拟目标项目 (模拟生成的子系统)
        var project = new Project
        {
            Id = DateTime.UtcNow.Ticks + 1,
            ProjectName = "AITestProject",
            ProjectPath = Path.Combine(Path.GetTempPath(), "AITestProject"),
            DatabaseType = CodeMaster.Core.Enums.DatabaseType.SQLite,
            ConnectionString = "Data Source=../AITestProject.Migrator/TargetDb.sqlite",
            Description = "本地AI辅助植入的测试项目"
        };
        db.Insertable(project).ExecuteCommand();

        // 2. 附加一个模块
        var module = new ProjectModule
        {
            Id = DateTime.UtcNow.Ticks + 2,
            ProjectId = project.Id,
            ModuleName = "SysAdmin",
            ModuleDescription = "系统管理模块",
            OrderNum = 1
        };
        db.Insertable(module).ExecuteCommand();

        // 3. 构造用于极限测试的实体
        var entity = new ModuleEntity
        {
            Id = DateTime.UtcNow.Ticks + 3,
            ModuleId = module.Id,
            Name = "Article",
            Description = "文章管理",
            TableName = "biz_article",
            HasPrimaryKey = true,
            IsTree = false,
            HasTenant = true, // 带上多租户
            HasDataPermission = true, // 带上数据权限
            GenerateFrontend = true,
            FrontendRoute = "/sysadmin/article",
            IsReadOnly = false
        };
        db.Insertable(entity).ExecuteCommand();

        // 4. 为其分配刁钻的属性集合 (覆盖所有你要测试的前端组件类型)
        var fields = new List<EntityField>
        {
            new EntityField { ModuleEntityId = entity.Id, Name = "Id", DataType = "long", IsPrimaryKey = true, FormControlType = "input", IsRequired = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "Title", DataType = "string", MaxLength = 100, FormControlType = "input", IsRequired = true, ShowInList = true, ShowInAddForm = true, ShowInEditForm = true, ShowInSearch = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "Content", DataType = "string", FormControlType = "editor", ShowInAddForm = true, ShowInEditForm = true, ShowInDetail = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "ArticleType", DataType = "int", FormControlType = "select", SelectDataSource = "dict", SelectOptions = "article_type", ShowInList = true, ShowInAddForm = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "Tags", DataType = "string", FormControlType = "checkbox-group", IsMultiple = true, SelectOptions = "[{\"label\":\"Tech\",\"value\":\"Tech\"},{\"label\":\"Life\",\"value\":\"Life\"}]", ShowInList = true, ShowInAddForm = true, ShowInEditForm = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "PublishStatus", DataType = "bool", FormControlType = "switch", ShowInList = true, ShowInAddForm = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "ViewCount", DataType = "int", FormControlType = "number", ShowInList = true, ShowInDetail = true },
            new EntityField { ModuleEntityId = entity.Id, Name = "PublishDate", DataType = "DateTime", FormControlType = "datetime", ShowInList = true, ShowInAddForm = true }
        };
        db.Insertable(fields).ExecuteCommand();

        // 完成了，跑完这个你就可以到 Vue 前端去查看这条 'AITestProject' 项目及相关的实体配置啦
    }
}
