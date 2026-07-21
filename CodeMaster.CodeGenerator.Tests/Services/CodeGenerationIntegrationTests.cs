using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using SqlSugar;
using Xunit;
using Yitter.IdGenerator;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class CodeGenerationIntegrationTests : IDisposable
{
    private readonly ISqlSugarClient _db;
    private readonly string _tempProjectPath;
    private readonly string _sqliteFile;

    public CodeGenerationIntegrationTests()
    {
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = 1 });

        _tempProjectPath = Path.Combine(Path.GetTempPath(), "CodeMaster_TestProj_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectPath);

        _sqliteFile = Path.Combine(_tempProjectPath, "test_codemaster.db");
        _db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"Data Source={_sqliteFile};Mode=ReadWriteCreate;",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        _db.CodeFirst.InitTables(
            typeof(ModuleEntity),
            typeof(EntityField),
            typeof(Project),
            typeof(ProjectModule),
            typeof(SysMenu),
            typeof(SysLangText),
            typeof(OneToManyRelation),
            typeof(EntityRelation),
            typeof(SysPageTemplate),
            typeof(SysFieldControlTemplate),
            typeof(SysChildTemplate));

        SeedMinimalTemplates();
    }

    [Fact]
    public async Task Test_GenerateCode_PartialClass_And_FileCompletion()
    {
        var testProject = new Project
        {
            Id = 1,
            ProjectName = "TestAutomationProject",
            ProjectPath = _tempProjectPath,
            DatabaseType = CodeMaster.Core.Enums.DatabaseType.SQLite,
            ConnectionString = $"Data Source={_sqliteFile}",
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(testProject).ExecuteCommand();

        var testModule = new ProjectModule
        {
            Id = 1,
            ProjectId = testProject.Id,
            ModuleName = "System",
            ModuleDescription = "System",
            OrderNum = 1,
            Icon = "folder",
            RoutePath = "/system",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(testModule).ExecuteCommand();

        var testEntity = new ModuleEntity
        {
            Id = 1,
            ProjectId = testProject.Id,
            ModuleId = testModule.Id,
            Name = "DemoAutomatedUser",
            Description = "Demo user",
            TableName = "demo_automated_user",
            HasPrimaryKey = true,
            GenerateFrontend = true,
            FrontendRoute = "/system/demo",
            IsReadOnly = false,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(testEntity).ExecuteCommand();

        var fields = new List<EntityField>
        {
            new() { Id = 101, ModuleEntityId = testEntity.Id, Name = "Id", DataType = "long", IsPrimaryKey = true, CreateBy = "test", UpdateUserId = 0 },
            new() { Id = 102, ModuleEntityId = testEntity.Id, Name = "Name", Description = "Name", DataType = "string", ShowInList = true, ShowInAddForm = true, ShowInEditForm = true, ShowInDetail = true, CreateBy = "test", UpdateUserId = 0 }
        };
        _db.Insertable(fields).ExecuteCommand();

        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);

        var generateResult = await service.GenerateCodeAsync(testEntity.Id);

        Assert.True(generateResult);

        var domainPath = Path.Combine(_tempProjectPath, $"{testProject.ProjectName}.Domain", "Entities", testModule.ModuleName);
        var dtoPath = Path.Combine(_tempProjectPath, $"{testProject.ProjectName}.Application", "Dtos", testModule.ModuleName);
        var autoFilePath = Path.Combine(domainPath, $"{testEntity.Name}.auto.cs");
        var userFilePath = Path.Combine(domainPath, $"{testEntity.Name}.cs");
        var dtoFilePath = Path.Combine(dtoPath, $"{testEntity.Name}Dto.cs");

        Assert.True(File.Exists(autoFilePath), ".auto.cs file was not generated.");
        Assert.True(File.Exists(userFilePath), "User .cs file was not generated.");
        Assert.True(File.Exists(dtoFilePath), "DTO file was not generated.");

        var frontendPath = Path.Combine(_tempProjectPath, $"{testProject.ProjectName}.Vue", "src", "views", "system", "demoAutomatedUser");
        Assert.True(File.Exists(Path.Combine(frontendPath, "index.vue")), "Index page was not generated.");
        Assert.True(File.Exists(Path.Combine(frontendPath, "add.vue")), "Add page was not generated.");
        Assert.True(File.Exists(Path.Combine(frontendPath, "edit.vue")), "Edit page was not generated.");
        Assert.True(File.Exists(Path.Combine(frontendPath, "detail.vue")), "Detail page was not generated.");

        var modifiedUserContent = "public partial class DemoAutomatedUser { // user code }";
        await File.WriteAllTextAsync(userFilePath, modifiedUserContent);

        var autoFileInitialLastWrite = File.GetLastWriteTime(autoFilePath);
        await Task.Delay(500);

        await service.GenerateCodeAsync(testEntity.Id);

        var currentUserContent = await File.ReadAllTextAsync(userFilePath);
        Assert.Equal(modifiedUserContent, currentUserContent);

        var autoFileSubsequentLastWrite = File.GetLastWriteTime(autoFilePath);
        Assert.True(autoFileSubsequentLastWrite > autoFileInitialLastWrite, ".auto.cs should be overwritten on regeneration.");

        var autoContent = await File.ReadAllTextAsync(autoFilePath);
        Assert.Contains("public partial class DemoAutomatedUser", autoContent);
        Assert.Contains("public string Name", autoContent);

        Assert.True(File.Exists(Path.Combine(frontendPath, "index.vue")), "Generated frontend files should remain after regeneration.");
    }

    [Fact]
    public async Task ProjectGeneration_FullProcessesAll_IncrementalProcessesOnlyChangedEntities()
    {
        var project = new Project
        {
            Id = 21,
            ProjectName = "BatchGenerationProject",
            ProjectPath = _tempProjectPath,
            DatabaseType = CodeMaster.Core.Enums.DatabaseType.SQLite,
            ConnectionString = $"Data Source={_sqliteFile}",
            CreateBy = "test",
            UpdateUserId = 0
        };
        var module = new ProjectModule
        {
            Id = 22,
            ProjectId = project.Id,
            ModuleName = "Batch",
            ModuleDescription = "Batch",
            Icon = "folder",
            RoutePath = "/batch",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var first = new ModuleEntity
        {
            Id = 23,
            ProjectId = project.Id,
            ModuleId = module.Id,
            Name = "FirstBatchEntity",
            Description = "First batch entity",
            IsReadOnly = true,
            HasPrimaryKey = false,
            HasAudit = false,
            HasSoftDelete = false,
            GenerateFrontend = false,
            IsChildTable = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var second = new ModuleEntity
        {
            Id = 24,
            ProjectId = project.Id,
            ModuleId = module.Id,
            Name = "SecondBatchEntity",
            Description = "Second batch entity",
            IsReadOnly = true,
            HasPrimaryKey = false,
            HasAudit = false,
            HasSoftDelete = false,
            GenerateFrontend = false,
            IsChildTable = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var firstField = new EntityField
        {
            Id = 25,
            ModuleEntityId = first.Id,
            Name = "Name",
            Description = "Name",
            DataType = "string",
            CreateBy = "test",
            UpdateUserId = 0
        };
        var secondField = new EntityField
        {
            Id = 26,
            ModuleEntityId = second.Id,
            Name = "Code",
            Description = "Code",
            DataType = "string",
            CreateBy = "test",
            UpdateUserId = 0
        };

        _db.Insertable(project).ExecuteCommand();
        _db.Insertable(module).ExecuteCommand();
        _db.Insertable(new[] { first, second }).ExecuteCommand();
        _db.Insertable(new[] { firstField, secondField }).ExecuteCommand();

        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);
        var fullInput = new CodeMaster.Application.Dtos.CodeGen.ProjectCodeGenerationDto { ProjectId = project.Id };

        Assert.True(await service.GenerateProjectCodeAsync(fullInput));
        Assert.Equal(new[] { first.Id, second.Id }, fullInput.EntityIds.OrderBy(id => id));

        var generatedFirst = _db.Queryable<ModuleEntity>().Where(entity => entity.Id == first.Id).First();
        secondField.UpdateTime = generatedFirst.LastGeneratedTime!.Value.AddSeconds(1);
        _db.Updateable(secondField).UpdateColumns(field => new { field.UpdateTime }).ExecuteCommand();

        var incrementalInput = new CodeMaster.Application.Dtos.CodeGen.ProjectCodeGenerationDto { ProjectId = project.Id };
        Assert.True(await service.GenerateProjectIncrementalCodeAsync(incrementalInput));
        Assert.Equal(new[] { second.Id }, incrementalInput.EntityIds);
    }

    [Fact]
    public async Task ProjectSynchronization_UpdatesAllMenusAndLanguages()
    {
        var project = new Project
        {
            Id = 31,
            ProjectName = "BatchSyncProject",
            ProjectPath = _tempProjectPath,
            DatabaseType = CodeMaster.Core.Enums.DatabaseType.SQLite,
            ConnectionString = $"Data Source={_sqliteFile};Mode=ReadWriteCreate;",
            CreateBy = "test",
            UpdateUserId = 0
        };
        var module = new ProjectModule
        {
            Id = 32,
            ProjectId = project.Id,
            ModuleName = "Catalog",
            ModuleDescription = "Catalog management",
            Icon = "folder",
            RoutePath = "/catalog",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var entity = new ModuleEntity
        {
            Id = 33,
            ProjectId = project.Id,
            ModuleId = module.Id,
            Name = "Product",
            Description = "Product",
            HasPrimaryKey = true,
            IsReadOnly = true,
            GenerateFrontend = true,
            FrontendRoute = "/catalog/product",
            CreateBy = "test",
            UpdateUserId = 0
        };
        var field = new EntityField
        {
            Id = 34,
            ModuleEntityId = entity.Id,
            Name = "ProductName",
            Description = "Product name",
            DataType = "string",
            CreateBy = "test",
            UpdateUserId = 0
        };

        _db.Insertable(project).ExecuteCommand();
        _db.Insertable(module).ExecuteCommand();
        _db.Insertable(entity).ExecuteCommand();
        _db.Insertable(field).ExecuteCommand();

        var targetDb = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = project.ConnectionString,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(DbType.Sqlite)
        });
        targetDb.CodeFirst.InitTables(typeof(SysMenu), typeof(SysLangText));

        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);
        var menuInput = new CodeMaster.Application.Dtos.CodeGen.ProjectCodeGenerationDto { ProjectId = project.Id };
        var languageInput = new CodeMaster.Application.Dtos.CodeGen.ProjectCodeGenerationDto { ProjectId = project.Id };

        Assert.True(await service.SyncProjectMenusToTargetAsync(menuInput));
        Assert.True(await service.SyncProjectLanguagesToTargetAsync(languageInput));
        Assert.Equal(new[] { entity.Id }, menuInput.EntityIds);
        Assert.Equal(new[] { entity.Id }, languageInput.EntityIds);
        Assert.True(targetDb.Queryable<SysMenu>().Any(menu => menu.Component == "catalog/product/index"));
        Assert.True(targetDb.Queryable<SysLangText>().Any(text =>
            text.LangKey == "productName" && text.LangCode == "zh-CN" && text.LangValue == "Product name"));
    }

    [Fact]
    public async Task Test_SelectTable_DisplayFields_Render_In_Main_List_And_Detail_When_Raw_Field_Hidden()
    {
        const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";

        _db.Insertable(new[]
        {
            new ProjectModule
            {
                Id = 11,
                ProjectId = 1,
                ModuleName = "OrderManagement",
                ModuleDescription = "Order management",
                Icon = "folder",
                RoutePath = "/order",
                LastSyncTime = DateTime.UtcNow,
                Remark = string.Empty,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new ProjectModule
            {
                Id = 12,
                ProjectId = 1,
                ModuleName = "CustomerManagement",
                ModuleDescription = "Customer management",
                Icon = "folder",
                RoutePath = "/customer",
                LastSyncTime = DateTime.UtcNow,
                Remark = string.Empty,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var orderEntity = new ModuleEntity
        {
            Id = 21,
            ProjectId = 1,
            ModuleId = 11,
            Name = "Order",
            Description = "Order",
            TableName = "orders",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var customerEntity = new ModuleEntity
        {
            Id = 22,
            ProjectId = 1,
            ModuleId = 12,
            Name = "Customer",
            Description = "Customer",
            TableName = "customers",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(new[] { orderEntity, customerEntity }).ExecuteCommand();

        var customerIdField = new EntityField
        {
            Id = 31,
            ModuleEntityId = orderEntity.Id,
            Name = "CustomerId",
            Description = "Customer",
            DataType = "string",
            FormControlType = "select-table",
            RelatedEntityName = "Customer",
            RelatedEntityIdField = "Id",
            RelatedEntityDisplayFields = "[\"Name\",\"Type\"]",
            ShowInList = false,
            ShowInDetail = false,
            ShowInSearch = false,
            ShowInAddForm = true,
            ShowInEditForm = true,
            IsMultiple = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(customerIdField).ExecuteCommand();

        _db.Insertable(new[]
        {
            new SysFieldControlTemplate
            {
                Id = 301,
                ControlType = "select-table",
                PageSection = "list",
                HtmlContent = "<el-table-column [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_col_[field.id]_[displayField.nameLower]\"><template #default=\"scope\">{{ getSelectLabel(scope.row.[field.nameLower], [field.relatedEntityNameLower]Options, '[displayField.name]') }}</template></el-table-column>",
                ScriptSections = emptyScript,
                Sort = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 302,
                ControlType = "select-table",
                PageSection = "detail",
                HtmlContent = "<el-descriptions-item [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_field_[field.id]_[displayField.nameLower]\">{{ getSelectLabel(detail.[field.nameLower], [field.relatedEntityNameLower]Options, '[displayField.name]') }}</el-descriptions-item>",
                ScriptSections = emptyScript,
                Sort = 2,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var generator = new TemplateCodeGenerator(_db);

        var index = await generator.GeneratePageAsync(orderEntity.Id, "index", "TestAutomationProject", "OrderManagement");
        var detail = await generator.GeneratePageAsync(orderEntity.Id, "detail", "TestAutomationProject", "OrderManagement");

        Assert.DoesNotContain("field.displayFields", index.VueContent);
        Assert.DoesNotContain("field.displayFields", detail.VueContent);
        Assert.Contains("gen_col_31_name", index.VueContent);
        Assert.Contains("gen_col_31_type", index.VueContent);
        Assert.Contains("getSelectLabel(scope.row.customerId, customerOptions, 'Name')", index.VueContent);
        Assert.Contains("getSelectLabel(scope.row.customerId, customerOptions, 'Type')", index.VueContent);
        Assert.Contains("gen_field_31_name", detail.VueContent);
        Assert.Contains("gen_field_31_type", detail.VueContent);
        Assert.Contains("getSelectLabel(detail.customerId, customerOptions, 'Name')", detail.VueContent);
        Assert.Contains("getSelectLabel(detail.customerId, customerOptions, 'Type')", detail.VueContent);
    }

    [Fact]
    public async Task Test_Editor_Field_Generates_Lifecycle_Imports_And_Form_Sync()
    {
        const string editorScript = """
{"imports":[{"path":"vue","destructured":"ref, watch, onMounted, onUnmounted"},{"path":"@wangeditor/editor","destructured":"createEditor, createToolbar"},{"path":"@wangeditor/editor/dist/css/style.css"}],"uses":[],"refs":[{"name":"[field.nameLower]EditorInstance","initialValue":"null"}],"reactives":[],"functions":[{"name":"init[field.name]Editor","async":true,"body":["const { createEditor, createToolbar } = await import('@wangeditor/editor');","await import('@wangeditor/editor/dist/css/style.css');","const editor = createEditor({","  selector: '#[field.nameLower]Editor',","  html: [field.formPrefix].[field.nameLower] || '',","  config: {","    placeholder: '[field.description]',","    onChange(editor) { [field.formPrefix].[field.nameLower] = editor.getHtml(); }","  }","});","createToolbar({ editor, selector: '#[field.nameLower]Toolbar', config: {} });","[field.nameLower]EditorInstance.value = editor;"]}],"hooks":[{"name":"onMounted","body":["init[field.name]Editor();"]},{"name":"onUnmounted","body":["if ([field.nameLower]EditorInstance.value) { [field.nameLower]EditorInstance.value.destroy(); }"]}],"computed":[],"watches":[{"source":"[field.formPrefix].[field.nameLower]","body":["const editor = [field.nameLower]EditorInstance.value;","const html = [field.formPrefix].[field.nameLower] || '';","if (editor && editor.getHtml() !== html) { editor.setHtml(html); }"]}]}
""";
        const string editorFormHtml = "<el-col :xs=\"24\" :sm=\"24\"><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] class=\"editor-form-item\" data-gen-id=\"gen_field_[field.id]\"><div class=\"editor-wrap\" style=\"width:100%;border:1px solid var(--el-border-color);border-radius:4px;overflow:hidden\"><div :id=\"'[field.nameLower]Toolbar'\" class=\"editor-toolbar\" style=\"border-bottom:1px solid var(--el-border-color)\"></div><div :id=\"'[field.nameLower]Editor'\" class=\"editor-content\" style=\"min-height:220px\"></div></div></el-form-item></el-col>";
        const string editorListHtml = "<el-table-column [field.prop] :label=\"$t('[field.labelKey]')\" min-width=\"240\" data-gen-id=\"gen_col_[field.id]\"><template #default=\"scope\"><div class=\"rich-text-cell\" style=\"max-height:72px;overflow:hidden;line-height:1.5\" v-html=\"scope.row.[field.nameLower] || '-'\"></div></template></el-table-column>";
        const string editorDetailHtml = "<el-descriptions-item :label=\"$t('[field.labelKey]')\" :span=\"2\" data-gen-id=\"gen_field_[field.id]\"><div class=\"rich-text-detail\" style=\"max-width:100%;overflow:auto;line-height:1.6\" v-html=\"detail.[field.nameLower] || '-'\"></div></el-descriptions-item>";

        _db.Insertable(new ProjectModule
        {
            Id = 211,
            ProjectId = 1,
            ModuleName = "ProductManagement",
            ModuleDescription = "Product management",
            Icon = "folder",
            RoutePath = "/product",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        var productEntity = new ModuleEntity
        {
            Id = 221,
            ProjectId = 1,
            ModuleId = 211,
            Name = "Product",
            Description = "Product",
            TableName = "products",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(productEntity).ExecuteCommand();

        _db.Insertable(new EntityField
        {
            Id = 231,
            ModuleEntityId = productEntity.Id,
            Name = "Detail",
            Description = "Detail",
            DataType = "string",
            FormControlType = "editor",
            ShowInList = true,
            ShowInAddForm = true,
            ShowInEditForm = true,
            ShowInDetail = true,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new SysFieldControlTemplate
            {
                Id = 241,
                ControlType = "editor",
                PageSection = "add",
                HtmlContent = editorFormHtml,
                ScriptSections = editorScript,
                Sort = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 242,
                ControlType = "editor",
                PageSection = "edit",
                HtmlContent = editorFormHtml,
                ScriptSections = editorScript,
                Sort = 2,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 243,
                ControlType = "editor",
                PageSection = "list",
                HtmlContent = editorListHtml,
                ScriptSections = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}",
                Sort = 3,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 244,
                ControlType = "editor",
                PageSection = "detail",
                HtmlContent = editorDetailHtml,
                ScriptSections = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}",
                Sort = 4,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var generator = new TemplateCodeGenerator(_db);

        var add = await generator.GeneratePageAsync(productEntity.Id, "add", "TestAutomationProject", "ProductManagement");
        var edit = await generator.GeneratePageAsync(productEntity.Id, "edit", "TestAutomationProject", "ProductManagement");
        var index = await generator.GeneratePageAsync(productEntity.Id, "index", "TestAutomationProject", "ProductManagement");
        var detail = await generator.GeneratePageAsync(productEntity.Id, "detail", "TestAutomationProject", "ProductManagement");

        Assert.Contains("<el-col :xs=\"24\" :sm=\"24\"", add.VueContent);
        Assert.Contains("class=\"editor-form-item\"", add.VueContent);
        Assert.Contains("style=\"min-height:220px\"", add.VueContent);

        Assert.Contains("import { ref, watch, onMounted, onUnmounted } from 'vue';", edit.MainScriptContent);
        Assert.Contains("const detailEditorInstance = ref(null);", edit.MainScriptContent);
        Assert.Contains("placeholder: 'Detail'", edit.MainScriptContent);
        Assert.Contains("onChange(editor) { form.detail = editor.getHtml(); }", edit.MainScriptContent);
        Assert.Contains("watch(() => form.detail", edit.MainScriptContent);
        Assert.Contains("editor.setHtml(html)", edit.MainScriptContent);
        Assert.Contains("onUnmounted(() =>", edit.MainScriptContent);
        Assert.DoesNotContain("$t(", edit.MainScriptContent);

        Assert.Contains("v-html=\"scope.row.detail || '-'\"", index.VueContent);
        Assert.Contains("class=\"rich-text-cell\"", index.VueContent);
        Assert.Contains("v-html=\"detail.detail || '-'\"", detail.VueContent);
        Assert.Contains(":span=\"2\"", detail.VueContent);
    }

    [Fact]
    public async Task Test_SelectTable_DisplayFields_Render_By_Related_Field_Type()
    {
        const string selectTableScript = "{\"imports\":[{\"path\":\"@/api/[field.relatedModuleNameLower]/[field.relatedEntityNameLower]\",\"destructured\":\"getList as get[field.relatedEntityName]List\"}],\"uses\":[],\"refs\":[{\"name\":\"[field.relatedEntityNameLower]Options\",\"initialValue\":\"[]\"}],\"reactives\":[],\"functions\":[],\"hooks\":[{\"name\":\"onMounted\",\"body\":[\"get[field.relatedEntityName]List().then(res => { [field.relatedEntityNameLower]Options.value = res.map(item => ({ value: item.[field.relatedEntityIdFieldLower], ...item })) })\"]}],\"computed\":[],\"watches\":[]}";

        _db.Insertable(new[]
        {
            new ProjectModule
            {
                Id = 111,
                ProjectId = 1,
                ModuleName = "OrderManagement",
                ModuleDescription = "Order management",
                Icon = "folder",
                RoutePath = "/order",
                LastSyncTime = DateTime.UtcNow,
                Remark = string.Empty,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new ProjectModule
            {
                Id = 112,
                ProjectId = 1,
                ModuleName = "ProductManagement",
                ModuleDescription = "Product management",
                Icon = "folder",
                RoutePath = "/product",
                LastSyncTime = DateTime.UtcNow,
                Remark = string.Empty,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var orderEntity = new ModuleEntity
        {
            Id = 121,
            ProjectId = 1,
            ModuleId = 111,
            Name = "Order",
            Description = "Order",
            TableName = "orders",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var productEntity = new ModuleEntity
        {
            Id = 122,
            ProjectId = 1,
            ModuleId = 112,
            Name = "Product",
            Description = "Product",
            TableName = "products",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(new[] { orderEntity, productEntity }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new EntityField
            {
                Id = 131,
                ModuleEntityId = orderEntity.Id,
                Name = "ProductId",
                Description = "Product",
                DataType = "string",
                FormControlType = "select-table",
                RelatedEntityName = "Product",
                RelatedEntityIdField = "Id",
                RelatedEntityDisplayFields = "[\"Name\",\"Picture\",\"PublishTime\",\"Status\",\"Enabled\"]",
                ShowInList = false,
                ShowInDetail = false,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 141,
                ModuleEntityId = productEntity.Id,
                Name = "Name",
                Description = "Name",
                DataType = "string",
                FormControlType = "input",
                OrderNum = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 142,
                ModuleEntityId = productEntity.Id,
                Name = "Picture",
                Description = "Picture",
                DataType = "string",
                FormControlType = "image",
                OrderNum = 2,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 143,
                ModuleEntityId = productEntity.Id,
                Name = "PublishTime",
                Description = "Publish time",
                DataType = "DateTime",
                FormControlType = "datetime",
                OrderNum = 3,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 144,
                ModuleEntityId = productEntity.Id,
                Name = "Status",
                Description = "Status",
                DataType = "string",
                FormControlType = "select",
                SelectDataSource = "dict",
                SelectOptions = "product_status",
                OrderNum = 4,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 145,
                ModuleEntityId = productEntity.Id,
                Name = "Enabled",
                Description = "Enabled",
                DataType = "bool",
                FormControlType = "switch",
                OrderNum = 5,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new SysFieldControlTemplate
            {
                Id = 331,
                ControlType = "select-table",
                PageSection = "list",
                HtmlContent = "<el-table-column [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_col_[field.id]_[displayField.nameLower]\"><template #default=\"scope\">[displayField.listContent]</template></el-table-column>",
                ScriptSections = selectTableScript,
                Sort = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 332,
                ControlType = "select-table",
                PageSection = "detail",
                HtmlContent = "<el-descriptions-item [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_field_[field.id]_[displayField.nameLower]\">[displayField.detailContent]</el-descriptions-item>",
                ScriptSections = selectTableScript,
                Sort = 2,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var generator = new TemplateCodeGenerator(_db);

        var index = await generator.GeneratePageAsync(orderEntity.Id, "index", "TestAutomationProject", "OrderManagement");
        var detail = await generator.GeneratePageAsync(orderEntity.Id, "detail", "TestAutomationProject", "OrderManagement");

        Assert.Contains("getSelectLabel(scope.row.productId, productOptions, 'name')", index.VueContent);
        Assert.Contains("<el-image", index.VueContent);
        Assert.Contains("getSelectDisplayValues(scope.row.productId, productOptions, 'picture')", index.VueContent);
        Assert.Contains("formatDate(value, true)", index.VueContent);
        Assert.Contains("getDictLabel(value, productStatusOptions)", index.VueContent);
        Assert.Contains("toDisplayBool(value)", index.VueContent);

        Assert.Contains("<el-image", detail.VueContent);
        Assert.Contains("getSelectDisplayValues(detail.productId, productOptions, 'picture')", detail.VueContent);
        Assert.Contains("getDictLabel(value, productStatusOptions)", detail.VueContent);
        Assert.Contains("toDisplayBool(value)", detail.VueContent);

        Assert.Contains("const getSelectDisplayValues = (val, options, field) =>", index.MainScriptContent);
        Assert.Contains("const toDisplayBool = (value) =>", index.MainScriptContent);
        Assert.Contains("const productStatusOptions = ref([])", index.MainScriptContent);
        Assert.Contains("product_status", index.MainScriptContent);
        Assert.Contains("getDataListByType", index.MainScriptContent);
    }

    [Fact]
    public async Task ChildUploadList_OnAddPage_ExportsDisplayHelpers()
    {
        const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";
        const long moduleId = 401;
        const long orderId = 402;
        const long orderItemId = 403;

        _db.Insertable(new ProjectModule
        {
            Id = moduleId,
            ProjectId = 1,
            ModuleName = "OrderManagement",
            ModuleDescription = "Order management",
            Icon = "folder",
            RoutePath = "/order",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new ModuleEntity
            {
                Id = orderId,
                ProjectId = 1,
                ModuleId = moduleId,
                Name = "Order",
                Description = "Order",
                TableName = "orders",
                GenerateFrontend = true,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new ModuleEntity
            {
                Id = orderItemId,
                ProjectId = 1,
                ModuleId = moduleId,
                Name = "OrderItem",
                Description = "Order item",
                TableName = "order_items",
                GenerateFrontend = true,
                IsChildTable = true,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        _db.Insertable(new EntityField
        {
            Id = 404,
            ModuleEntityId = orderItemId,
            Name = "ProductImage",
            Description = "Product image",
            DataType = "string",
            FormControlType = "image",
            ShowInList = true,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new OneToManyRelation
        {
            Id = 405,
            ModuleEntityId = orderId,
            MasterField = "Id",
            ChildEntityId = orderItemId,
            ChildEntityName = "OrderItem",
            ChildForeignKey = "OrderId",
            OrderNum = 1,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new SysFieldControlTemplate
        {
            Id = 406,
            ControlType = "image",
            PageSection = "list",
            HtmlContent = "<el-table-column><template #default=\"scope\"><div v-if=\"getUploadValues(scope.row.productImage).length\">{{ getUploadFileName(scope.row.productImage) }}</div></template></el-table-column>",
            ScriptSections = emptyScript,
            Sort = 1,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new SysChildTemplate
        {
            Id = 407,
            PageType = "add",
            ChildType = "card",
            HtmlContent = "<el-table>[relation.tableColumns]</el-table>",
            ScriptSections = emptyScript,
            Sort = 1,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        var generator = new TemplateCodeGenerator(_db);
        var add = await generator.GeneratePageAsync(orderId, "add", "TestAutomationProject", "OrderManagement");

        Assert.Contains("getUploadValues(scope.row.productImage)", add.VueContent);
        Assert.Contains("const getUploadValues = (value) =>", add.MainScriptContent);
        Assert.Contains("const getUploadFileName = (url, index = 0) =>", add.MainScriptContent);
        Assert.Contains("getUploadValues", add.VueImportLine);
        Assert.Contains("getUploadFileName", add.VueImportLine);
    }

    [Fact]
    public async Task Test_Computed_And_Aggregate_Fields_Generate_Auto_Calc_Scripts()
    {
        const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";
        const string childDialogScript = "{\"imports\":[{\"path\":\"vue\",\"destructured\":\"reactive\"}],\"uses\":[],\"refs\":[],\"reactives\":[{\"name\":\"[relation.formName]\",\"fields\":{}}],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";

        _db.Insertable(new ProjectModule
        {
            Id = 51,
            ProjectId = 1,
            ModuleName = "OrderManagement",
            ModuleDescription = "Order management",
            Icon = "folder",
            RoutePath = "/order",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        var orderEntity = new ModuleEntity
        {
            Id = 61,
            ProjectId = 1,
            ModuleId = 51,
            Name = "Order",
            Description = "Order",
            TableName = "orders",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var orderItemEntity = new ModuleEntity
        {
            Id = 62,
            ProjectId = 1,
            ModuleId = 51,
            Name = "OrderItem",
            Description = "Order item",
            TableName = "order_items",
            GenerateFrontend = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(new[] { orderEntity, orderItemEntity }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new EntityField
            {
                Id = 71,
                ModuleEntityId = orderEntity.Id,
                Name = "TotalPrice",
                Description = "Total price",
                DataType = "decimal",
                FormControlType = "number",
                FieldCategory = "Aggregate",
                AggregateType = "Sum",
                AggregateChildEntityId = orderItemEntity.Id,
                AggregateChildFieldName = "LineTotal",
                ShowInAddForm = true,
                ShowInEditForm = true,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 72,
                ModuleEntityId = orderItemEntity.Id,
                Name = "Price",
                Description = "Price",
                DataType = "decimal",
                FormControlType = "number",
                IsRequired = true,
                IsNullable = true,
                ShowInAddForm = true,
                ShowInEditForm = true,
                OrderNum = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 73,
                ModuleEntityId = orderItemEntity.Id,
                Name = "Quantity",
                Description = "Quantity",
                DataType = "int",
                FormControlType = "number",
                ShowInAddForm = true,
                ShowInEditForm = true,
                OrderNum = 2,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new EntityField
            {
                Id = 74,
                ModuleEntityId = orderItemEntity.Id,
                Name = "LineTotal",
                Description = "Line total",
                DataType = "decimal",
                FormControlType = "number",
                FieldCategory = "Computed",
                Formula = "[Price]*[Quantity]",
                ShowInAddForm = true,
                ShowInEditForm = true,
                OrderNum = 3,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        _db.Insertable(new OneToManyRelation
        {
            Id = 81,
            ModuleEntityId = orderEntity.Id,
            MasterField = "Id",
            ChildEntityId = orderItemEntity.Id,
            ChildEntityName = "OrderItem",
            ChildForeignKey = "OrderId",
            OrderNum = 1,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new SysFieldControlTemplate
            {
                Id = 91,
                ControlType = "number",
                PageSection = "add",
                HtmlContent = "<el-form-item [field.prop]><el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item>",
                ScriptSections = emptyScript,
                Sort = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysFieldControlTemplate
            {
                Id = 92,
                ControlType = "number",
                PageSection = "edit",
                HtmlContent = "<el-form-item [field.prop]><el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item>",
                ScriptSections = emptyScript,
                Sort = 2,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        _db.Insertable(new[]
        {
            new SysChildTemplate
            {
                Id = 101,
                PageType = "add",
                ChildType = "dialog",
                HtmlContent = "[relation.dialogColumns]",
                ScriptSections = childDialogScript,
                Sort = 1,
                CreateBy = "test",
                UpdateUserId = 0
            },
            new SysChildTemplate
            {
                Id = 102,
                PageType = "edit",
                ChildType = "dialog",
                HtmlContent = "[relation.dialogColumns]",
                ScriptSections = childDialogScript,
                Sort = 2,
                CreateBy = "test",
                UpdateUserId = 0
            }
        }).ExecuteCommand();

        var generator = new TemplateCodeGenerator(_db);
        var add = await generator.GeneratePageAsync(orderEntity.Id, "add", "TestAutomationProject", "OrderManagement");

        Assert.Contains("v-model=\"form.totalPrice\"", add.VueContent);
        Assert.Matches("v-model=\"form\\.totalPrice\"[^>]*disabled", add.VueContent);
        Assert.Matches("v-model=\"orderItemForm\\.lineTotal\"[^>]*disabled", add.VueContent);
        Assert.Contains("prop=\"price\"", add.VueContent);
        Assert.DoesNotContain("prop=\"orderItemForm.price\"", add.VueContent);
        Assert.Contains(":rules=\"[{ required: true, message: '请输入Price', trigger: 'blur' }]\"", add.VueContent);
        Assert.Contains("const calcTotalPrice = () =>", add.MainScriptContent);
        Assert.Contains("form.totalPrice = normalizeCalcValue(rows.reduce((sum, item) => sum + toCalcNumber(item.lineTotal), 0))", add.MainScriptContent);
        Assert.Contains("watch(() => form.orderItems", add.MainScriptContent);

        var childScript = Assert.Single(add.ChildScripts.Values);
        Assert.Contains("const orderItemForm = reactive({  });", childScript.ScriptContent);
        Assert.Contains("const calcLineTotal = () =>", childScript.ScriptContent);
        Assert.Contains("orderItemForm.lineTotal = normalizeCalcValue(toCalcNumber(orderItemForm.price)*toCalcNumber(orderItemForm.quantity))", childScript.ScriptContent);
        Assert.Contains("watch(() => [orderItemForm.price, orderItemForm.quantity]", childScript.ScriptContent);
    }

    [Fact]
    public async Task Create_SynchronizesAllEnabledSystemFields()
    {
        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);
        var entityId = await service.CreateAsync(new CodeMaster.Application.Dtos.CodeGen.CreateModuleEntityDto
        {
            ProjectId = 1,
            ModuleId = 1,
            Name = "SystemFieldEntity",
            Description = "System field entity",
            HasPrimaryKey = true,
            IsTree = true,
            HasTenant = true,
            HasDataPermission = true,
            HasAudit = true,
            HasSoftDelete = true,
            Fields =
            {
                new CodeMaster.Application.Dtos.CodeGen.CreateEntityFieldDto
                {
                    Name = "Code",
                    Description = "Code",
                    DataType = "string"
                }
            }
        });

        var fields = _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(field => field.ModuleEntityId == entityId && !field.IsDeleted)
            .ToList();
        var names = fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Code", names);
        Assert.Contains("Id", names);
        Assert.Contains("ParentId", names);
        Assert.Contains("Ancestors", names);
        Assert.Contains("TenantId", names);
        Assert.Contains("DeptId", names);
        Assert.Contains("DeptAncestors", names);
        Assert.Contains("CreateUserId", names);
        Assert.Contains("CreateBy", names);
        Assert.Contains("CreateTime", names);
        Assert.Contains("UpdateUserId", names);
        Assert.Contains("UpdateBy", names);
        Assert.Contains("UpdateTime", names);
        Assert.Contains("IsDeleted", names);
        Assert.Contains("DeleteUserId", names);
        Assert.Contains("DeleteBy", names);
        Assert.Contains("DeleteTime", names);
        Assert.All(fields.Where(field => field.Name != "Code"), field => Assert.True(field.IsSystemField));
    }

    [Fact]
    public async Task GenerateCode_RepairsMissingSystemFieldsForLegacyEntity()
    {
        var project = new Project
        {
            Id = 801,
            ProjectName = "LegacyRepairProject",
            ProjectPath = _tempProjectPath,
            DatabaseType = CodeMaster.Core.Enums.DatabaseType.SQLite,
            ConnectionString = $"Data Source={_sqliteFile}",
            CreateBy = "test",
            UpdateUserId = 0
        };
        var module = new ProjectModule
        {
            Id = 802,
            ProjectId = project.Id,
            ModuleName = "Legacy",
            ModuleDescription = "Legacy",
            OrderNum = 1,
            Icon = "folder",
            RoutePath = "/legacy",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var entity = new ModuleEntity
        {
            Id = 803,
            ProjectId = project.Id,
            ModuleId = module.Id,
            Name = "LegacyEntity",
            Description = "Legacy entity",
            TableName = "legacy_entity",
            HasPrimaryKey = true,
            HasAudit = true,
            HasSoftDelete = true,
            GenerateFrontend = false,
            IsChildTable = true,
            CreateBy = "test",
            UpdateUserId = 0
        };
        var businessField = new EntityField
        {
            Id = 804,
            ModuleEntityId = entity.Id,
            Name = "Code",
            Description = "Code",
            DataType = "string",
            CreateBy = "test",
            UpdateUserId = 0
        };

        _db.Insertable(project).ExecuteCommand();
        _db.Insertable(module).ExecuteCommand();
        _db.Insertable(entity).ExecuteCommand();
        _db.Insertable(businessField).ExecuteCommand();

        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);
        Assert.True(await service.GenerateCodeAsync(entity.Id));

        var fields = _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(field => field.ModuleEntityId == entity.Id && !field.IsDeleted)
            .ToList();
        var names = fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Id", names);
        Assert.Contains("CreateTime", names);
        Assert.Contains("UpdateTime", names);
        Assert.Contains("IsDeleted", names);
        Assert.Contains("DeleteTime", names);

        var autoPath = Path.Combine(
            _tempProjectPath,
            $"{project.ProjectName}.Domain",
            "Entities",
            module.ModuleName,
            $"{entity.Name}.auto.cs");
        var autoCode = await File.ReadAllTextAsync(autoPath);
        Assert.Contains("public long Id", autoCode);
        Assert.Contains("public DateTime CreateTime", autoCode);
        Assert.Contains("public bool IsDeleted", autoCode);
    }

    [Fact]
    public async Task Update_TogglesAndRestoresAuditAndSoftDeleteSystemFields()
    {
        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);
        var entityId = await service.CreateAsync(new CodeMaster.Application.Dtos.CodeGen.CreateModuleEntityDto
        {
            ProjectId = 1,
            ModuleId = 1,
            Name = "ToggleSystemFieldEntity",
            Description = "Toggle system fields",
            HasPrimaryKey = true,
            HasAudit = true,
            HasSoftDelete = true
        });
        var originalAuditIds = _db.Queryable<EntityField>()
            .Where(field => field.ModuleEntityId == entityId &&
                            (field.Name == "CreateUserId" || field.Name == "CreateBy" ||
                             field.Name == "CreateTime" || field.Name == "UpdateUserId" ||
                             field.Name == "UpdateBy" || field.Name == "UpdateTime"))
            .ToList()
            .ToDictionary(field => field.Name, field => field.Id, StringComparer.OrdinalIgnoreCase);

        await service.UpdateAsync(entityId, new CodeMaster.Application.Dtos.CodeGen.UpdateModuleEntityDto
        {
            Name = "ToggleSystemFieldEntity",
            Description = "Toggle system fields",
            HasPrimaryKey = true,
            HasAudit = false,
            HasSoftDelete = false
        });

        var disabledFields = _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(field => field.ModuleEntityId == entityId)
            .ToList();
        Assert.Contains(disabledFields, field => field.Name == "Id" && !field.IsDeleted);
        Assert.DoesNotContain(disabledFields, field =>
            !field.IsDeleted && (field.Name.StartsWith("Create", StringComparison.Ordinal) ||
                                 field.Name.StartsWith("Update", StringComparison.Ordinal) ||
                                 field.Name.StartsWith("Delete", StringComparison.Ordinal) ||
                                 field.Name == "IsDeleted"));

        await service.UpdateAsync(entityId, new CodeMaster.Application.Dtos.CodeGen.UpdateModuleEntityDto
        {
            Name = "ToggleSystemFieldEntity",
            Description = "Toggle system fields",
            HasPrimaryKey = true,
            HasAudit = true,
            HasSoftDelete = true
        });

        var restoredFields = _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(field => field.ModuleEntityId == entityId && !field.IsDeleted)
            .ToList();
        foreach (var item in originalAuditIds)
            Assert.Contains(restoredFields, field => field.Name == item.Key && field.Id == item.Value);
        Assert.Contains(restoredFields, field => field.Name == "DeleteUserId");
        Assert.Contains(restoredFields, field => field.Name == "IsDeleted");
    }

    [Fact]
    public async Task Test_Update_Automated_Field_Management()
    {
        var service = new ModuleEntityService(new DummyModuleEntityRepository(), new DummyExcelService(), _db, null);

        var testEntity = new ModuleEntity
        {
            Id = GenerateSnowflakeId(),
            ProjectId = 1,
            ModuleId = 1,
            Name = "FieldTestEntity",
            Description = "Field test",
            TableName = "test_field_entity",
            HasPrimaryKey = false,
            IsReadOnly = true,
            HasAudit = false,
            HasSoftDelete = false,
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(testEntity).ExecuteCommand();

        var existingField = new EntityField
        {
            Id = GenerateSnowflakeId(),
            ModuleEntityId = testEntity.Id,
            Name = "OldField",
            DataType = "string",
            CreateBy = "test",
            UpdateUserId = 0
        };
        _db.Insertable(existingField).ExecuteCommand();

        var updateDto = new CodeMaster.Application.Dtos.CodeGen.UpdateModuleEntityDto
        {
            Name = "FieldTestEntity_Updated",
            Description = "Field test",
            TableName = "test_field_entity",
            NewFields = new List<CodeMaster.Application.Dtos.CodeGen.CreateEntityFieldDto>
            {
                new() { Name = "NewAutoGeneratedField", DataType = "long", IsSystemField = true }
            },
            UpdatedFields = new List<CodeMaster.Application.Dtos.CodeGen.UpdateEntityFieldWithIdDto>
            {
                new() { Id = existingField.Id, Name = "OldFieldRenamed", DataType = "string" }
            },
            DeletedFieldIds = new List<long> { existingField.Id }
        };

        var rowCount = await service.UpdateAsync(testEntity.Id, updateDto);

        Assert.Equal(1, rowCount);

        var finalEntity = _db.Queryable<ModuleEntity>().InSingle(testEntity.Id);
        Assert.Equal("FieldTestEntity_Updated", finalEntity.Name);

        var dbFields = _db.Queryable<EntityField>().Where(f => f.ModuleEntityId == testEntity.Id && f.IsDeleted == false).ToList();
        Assert.Single(dbFields);
        Assert.Equal("NewAutoGeneratedField", dbFields[0].Name);
        Assert.DoesNotContain(dbFields, f => f.Id == existingField.Id);

        var deletedField = _db.Queryable<EntityField>().Where(f => f.Id == existingField.Id).First();
        Assert.True(deletedField.IsDeleted);
        Assert.NotNull(deletedField.DeleteTime);
    }

    [Fact]
    public async Task OwnedOne_GeneratesNestedPagesDtosAndTransactionalService()
    {
        const long projectId = 8100;
        const long moduleId = 8101;
        const long orderId = 8102;
        const long detailId = 8103;
        const long relationId = 8104;

        _db.Insertable(new ProjectModule
        {
            Id = moduleId,
            ProjectId = projectId,
            ModuleName = "OrderManagement",
            ModuleDescription = "订单管理",
            Icon = "folder",
            RoutePath = "/order",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();
        _db.Insertable(new[]
        {
            new ModuleEntity
            {
                Id = orderId, ProjectId = projectId, ModuleId = moduleId, Name = "Order", Description = "订单",
                TableName = "order", HasPrimaryKey = true, HasSoftDelete = true, CreateBy = "test", UpdateUserId = 0
            },
            new ModuleEntity
            {
                Id = detailId, ProjectId = projectId, ModuleId = moduleId, Name = "OrderDetail", Description = "订单详情",
                TableName = "order_detail", HasPrimaryKey = true, HasSoftDelete = true, IsChildTable = true,
                CreateBy = "test", UpdateUserId = 0
            },
            new ModuleEntity
            {
                Id = 8105, ProjectId = projectId, ModuleId = moduleId, Name = "Customer", Description = "客户",
                TableName = "customer", HasPrimaryKey = true, CreateBy = "test", UpdateUserId = 0
            }
        }).ExecuteCommand();
        _db.Insertable(new[]
        {
            new EntityField
            {
                Id = 8110, ModuleEntityId = orderId, Name = "Id", Description = "主键", DataType = "long",
                IsPrimaryKey = true, IsRequired = true, CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8111, ModuleEntityId = detailId, Name = "Id", Description = "主键", DataType = "long",
                IsPrimaryKey = true, IsRequired = true, CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8112, ModuleEntityId = detailId, Name = "OrderId", Description = "订单ID", DataType = "long",
                IsRequired = true, CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8113, ModuleEntityId = detailId, Name = "ReceiverName", Description = "收货人", DataType = "string",
                FormControlType = "input", ShowInList = true, ShowInAddForm = true, ShowInEditForm = true,
                ShowInDetail = true, IsRequired = true, IsNullable = true, CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8114, ModuleEntityId = detailId, Name = "CustomerId", Description = "客户", DataType = "long",
                FormControlType = "select-table", RelatedEntityName = "Customer", RelatedEntityIdField = "Id",
                RelatedEntityDisplayFields = "[\"Name\"]",
                ResultMappings = "[{\"sourceField\":\"Name\",\"targetField\":\"ReceiverName\"}]",
                ShowInAddForm = true, ShowInEditForm = true, ShowInDetail = true,
                CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8115, ModuleEntityId = 8105, Name = "Id", Description = "主键", DataType = "long",
                IsPrimaryKey = true, IsRequired = true, CreateBy = "test", UpdateUserId = 0
            },
            new EntityField
            {
                Id = 8116, ModuleEntityId = 8105, Name = "Name", Description = "客户名称", DataType = "string",
                ShowInList = true, CreateBy = "test", UpdateUserId = 0
            }
        }).ExecuteCommand();
        _db.Insertable(new EntityRelation
        {
            Id = relationId,
            SourceEntityId = orderId,
            TargetEntityId = detailId,
            RelationName = "Detail",
            SourceField = "Id",
            TargetField = "OrderId",
            Cardinality = EntityRelationCardinality.OneToOne,
            Ownership = EntityRelationOwnership.Owned,
            DeleteBehavior = EntityRelationDeleteBehavior.Delete,
            OrderNum = 10,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";
        const string selectTableScript = "{\"imports\":[],\"uses\":[],\"refs\":[{\"name\":\"[field.relatedEntityNameLower]Options\",\"initialValue\":\"[]\"}],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";
        _db.Insertable(new[]
        {
            new SysFieldControlTemplate { Id = 8120, ControlType = "input", PageSection = "add", HtmlContent = "<el-col><el-form-item [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item></el-col>", ScriptSections = emptyScript, Sort = 1, CreateBy = "test", UpdateUserId = 0 },
            new SysFieldControlTemplate { Id = 8121, ControlType = "input", PageSection = "edit", HtmlContent = "<el-col><el-form-item [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item></el-col>", ScriptSections = emptyScript, Sort = 2, CreateBy = "test", UpdateUserId = 0 },
            new SysFieldControlTemplate { Id = 8122, ControlType = "table-column", PageSection = "list", HtmlContent = "<el-table-column [field.prop] data-gen-id=\"gen_col_[field.id]\"><template #default=\"scope\">{{ [field.rowPrefix].[field.nameLower] }}</template></el-table-column>", ScriptSections = emptyScript, Sort = 3, CreateBy = "test", UpdateUserId = 0 },
            new SysFieldControlTemplate { Id = 8123, ControlType = "input", PageSection = "detail", HtmlContent = "<el-descriptions-item data-gen-id=\"gen_field_[field.id]\">{{ [field.detailPrefix].[field.nameLower] }}</el-descriptions-item>", ScriptSections = emptyScript, Sort = 4, CreateBy = "test", UpdateUserId = 0 },
            new SysFieldControlTemplate { Id = 8124, ControlType = "select-table", PageSection = "add", HtmlContent = "<el-col><el-form-item [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower]\"></el-select></el-form-item></el-col>", ScriptSections = selectTableScript, Sort = 5, CreateBy = "test", UpdateUserId = 0 },
            new SysFieldControlTemplate { Id = 8125, ControlType = "select-table", PageSection = "edit", HtmlContent = "<el-col><el-form-item [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower]\"></el-select></el-form-item></el-col>", ScriptSections = selectTableScript, Sort = 6, CreateBy = "test", UpdateUserId = 0 }
        }).ExecuteCommand();

        var pageGenerator = new TemplateCodeGenerator(_db);
        var add = await pageGenerator.GeneratePageAsync(orderId, "add", "TestAutomationProject", "OrderManagement");
        var edit = await pageGenerator.GeneratePageAsync(orderId, "edit", "TestAutomationProject", "OrderManagement");
        var index = await pageGenerator.GeneratePageAsync(orderId, "index", "TestAutomationProject", "OrderManagement");
        var detail = await pageGenerator.GeneratePageAsync(orderId, "detail", "TestAutomationProject", "OrderManagement");

        Assert.Contains("form.detail.receiverName", add.VueContent);
        Assert.Contains("form.detail.receiverName", edit.VueContent);
        Assert.Contains(":rules=\"[{ required: true, message: '请输入收货人', trigger: 'blur' }]\"", add.VueContent);
        Assert.Contains("handleForm_detailCustomerIdSelection", add.VueContent);
        Assert.Contains("form.detail.receiverName = selected ? selected.name : null", add.MainScriptContent);
        Assert.Contains("scope.row.detail.receiverName", index.VueContent);
        Assert.Contains("detail.detail.receiverName", detail.VueContent);
        Assert.Contains($"gen_owned_{relationId}", add.VueContent);

        var generator = new CodeGeneratorService(_db);
        var entityCode = await generator.GenerateEntityAutoAsync(orderId, "TestAutomationProject", "OrderManagement");
        var dtoCode = await generator.GenerateDtoAsync(orderId, "TestAutomationProject", "OrderManagement");
        var serviceCode = await generator.GenerateServiceImplementationAsync(orderId, "TestAutomationProject", "OrderManagement");

        Assert.Contains("[SugarColumn(IsIgnore = true)]", entityCode);
        Assert.DoesNotContain("NavigateType.OneToOne", entityCode);
        Assert.Contains("CreateOrderDetailDto? Detail", dtoCode);
        Assert.Contains("_db.Ado.BeginTran()", serviceCode);
        Assert.Contains("LoadOwnedRelationsAsync", serviceCode);
        Assert.Contains("detailRows", serviceCode);
        Assert.Contains("detailSourceValues.Contains(item.OrderId)", serviceCode);
        Assert.DoesNotContain("Includes(e => e.Detail)", serviceCode);
        Assert.Contains("SaveDetailAsync", serviceCode);
        Assert.Contains("ownedDetail.OrderId = id", serviceCode);
    }

    private void SeedMinimalTemplates()
    {
        const string emptyScript = "{\"imports\":[],\"uses\":[],\"refs\":[],\"reactives\":[],\"functions\":[],\"hooks\":[],\"computed\":[],\"watches\":[]}";
        var pages = new[]
        {
            new SysPageTemplate { Id = 201, PageType = "index", Name = "index", HtmlContent = "<div>[gen.searchColumns][gen.listColumns]</div>", ScriptSections = emptyScript, Sort = 1, CreateBy = "test", UpdateUserId = 0 },
            new SysPageTemplate { Id = 202, PageType = "add", Name = "add", HtmlContent = "<div>[gen.addColumns][gen.relationCards][gen.relationDialogs]</div>", ScriptSections = emptyScript, Sort = 2, CreateBy = "test", UpdateUserId = 0 },
            new SysPageTemplate { Id = 203, PageType = "edit", Name = "edit", HtmlContent = "<div>[gen.editColumns][gen.relationCards][gen.relationDialogs]</div>", ScriptSections = emptyScript, Sort = 3, CreateBy = "test", UpdateUserId = 0 },
            new SysPageTemplate { Id = 204, PageType = "detail", Name = "detail", HtmlContent = "<div>[gen.detailColumns][gen.relationCards]</div>", ScriptSections = emptyScript, Sort = 4, CreateBy = "test", UpdateUserId = 0 }
        };
        _db.Insertable(pages).ExecuteCommand();
    }

    private long GenerateSnowflakeId() => DateTime.UtcNow.Ticks + new Random().Next(100, 999);

    public void Dispose()
    {
        _db.Ado.Close();
        _db.Dispose();

        if (Directory.Exists(_tempProjectPath))
        {
            try { Directory.Delete(_tempProjectPath, true); } catch { /* ignore in tear down */ }
        }
    }

    private class DummyModuleEntityRepository : IRepository<ModuleEntity>
    {
        public ISugarQueryable<ModuleEntity> GetQueryable() => null!;
        public ISugarQueryable<ModuleEntity> GetQueryable<ModuleEntity>() => null!;
        public IQueryable<ModuleEntity> AsQueryable() => new List<ModuleEntity>().AsQueryable();
        public ModuleEntity? GetById(long id) => null;
        public Task<ModuleEntity?> GetByIdAsync(long id) => Task.FromResult<ModuleEntity?>(null);
        public List<ModuleEntity> GetList(Expression<Func<ModuleEntity, bool>>? where = null) => new();
        public Task<List<ModuleEntity>> GetListAsync(Expression<Func<ModuleEntity, bool>>? where = null) => Task.FromResult(new List<ModuleEntity>());
        public (List<ModuleEntity> Items, int Total) GetPagedList(Expression<Func<ModuleEntity, bool>>? where, int pageNum, int pageSize) => (new(), 0);
        public Task<(List<ModuleEntity> Items, int Total)> GetPagedListAsync(Expression<Func<ModuleEntity, bool>>? where, int pageNum, int pageSize) => Task.FromResult((new List<ModuleEntity>(), 0));
        public bool Any(Expression<Func<ModuleEntity, bool>> where) => false;
        public Task<bool> AnyAsync(Expression<Func<ModuleEntity, bool>> where) => Task.FromResult(false);
        public int Count(Expression<Func<ModuleEntity, bool>>? where = null) => 0;
        public Task<int> CountAsync(Expression<Func<ModuleEntity, bool>>? where = null) => Task.FromResult(0);
        public long Insert(ModuleEntity entity) => 0;
        public Task<long> InsertAsync(ModuleEntity entity) => Task.FromResult(0L);
        public int InsertRange(List<ModuleEntity> entities) => 0;
        public Task<int> InsertRangeAsync(List<ModuleEntity> entities) => Task.FromResult(0);
        public int Update(ModuleEntity entity) => 0;
        public Task<int> UpdateAsync(ModuleEntity entity) => Task.FromResult(0);
        public int Delete(long id) => 0;
        public Task<int> DeleteAsync(long id) => Task.FromResult(0);
        public int Delete(Expression<Func<ModuleEntity, bool>> where) => 0;
        public Task<int> DeleteAsync(Expression<Func<ModuleEntity, bool>> where) => Task.FromResult(0);
    }

    private class DummyExcelService : IExcelService
    {
        public Task<byte[]> ExportAsync<T>(List<T> data, string sheetName = "Sheet1") where T : class
            => Task.FromResult(Array.Empty<byte>());

        public Task<List<T>> ImportAsync<T>(byte[] fileBytes, string sheetName = "Sheet1") where T : class, new()
            => Task.FromResult(new List<T>());
    }
}
