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
            typeof(OneToManyRelation),
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
                HtmlContent = "<el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" />",
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
                HtmlContent = "<el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" />",
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
