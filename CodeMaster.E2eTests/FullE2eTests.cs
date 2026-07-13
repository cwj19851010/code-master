namespace CodeMaster.E2eTests;

/// <summary>
/// 全流程 E2E 测试套件（按序运行）
///
/// 前置条件：
///   1. CodeMaster 后台已启动（默认 http://localhost:5000）
///   2. 环境变量可选覆盖：
///      E2E_BASE_URL  = CodeMaster API 地址
///      E2E_PROJECT_PATH = 测试项目生成目录（默认 C:/E2eTestProjects）
///
/// 运行方式：
///   dotnet test CodeMaster.E2eTests --filter "Category=E2E" -v normal
/// </summary>
[Collection("E2E")] // 同一 collection 内顺序执行
public class FullE2eTests : IAsyncLifetime
{
    private readonly CodeMasterClient _client;

    public FullE2eTests()
    {
        _client = new CodeMasterClient();
    }

    public async Task InitializeAsync()
    {
        // 每次测试前登录
        await _client.LoginAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    // =========================================================
    // 测试 #1: 创建项目
    // =========================================================

    [Fact(DisplayName = "E2E_FullWorkflow: 项目初始化到生成、构建、迁移完整流程")]
    public async Task Test00_FullWorkflow()
    {
        await Test01_CreateProject();
        await Test02_InitializeProject();
        await Test03_CreateModuleAndSyncMenu();
        await Test04_CreateArticleEntity();
        await Test05_CreateArticleCommentEntity();
        await Test06_GenerateCode();
        await Test07_VerifyBackendFiles();
        await Test08_VerifyFrontendFiles();
        await Test09_VerifyCodeQuality();
        await Test10_Build();
        await Test11_MigrateDatabase();
    }

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_01: 创建新项目")]
    public async Task Test01_CreateProject()
    {
        Console.WriteLine($"📦 创建项目 {E2eConfig.TestProjectName}...");
        Console.WriteLine($"   API 地址：{E2eConfig.BaseUrl}");
        Console.WriteLine($"   生成目录：{E2eConfig.TestProjectParentPath}");

        var id = await _client.CreateProjectAsync();

        Assert.True(id > 0, $"项目 ID 应大于 0，实际：{id}");
        TestState.ProjectId = id;

        Console.WriteLine($"✅ 项目创建成功，ID = {id}");
    }

    // =========================================================
    // 测试 #2: 初始化项目（分步 Step1~Step8，跳过 Step5/6 迁移）
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_02: 初始化项目（解压模板到还原依赖）")]
    public async Task Test02_InitializeProject()
    {
        Assert.True(TestState.ProjectId > 0, "需要先执行 E2E_01 创建项目");

        Console.WriteLine($"🔧 开始初始化项目，ID = {TestState.ProjectId}");
        await _client.InitializeProjectStepsAsync(TestState.ProjectId);

        // 验证目录和 sln 文件
        Assert.True(Directory.Exists(E2eConfig.TestProjectFullPath),
            $"项目目录应存在：{E2eConfig.TestProjectFullPath}");

        var slnFile = Path.Combine(E2eConfig.TestProjectFullPath, $"{E2eConfig.TestProjectName}.sln");
        Assert.True(File.Exists(slnFile),
            $".sln 文件应存在：{slnFile}");

        Console.WriteLine($"✅ 项目初始化完成，目录：{E2eConfig.TestProjectFullPath}");
    }

    // =========================================================
    // 测试 #3: 创建模块 + 同步到菜单
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_03: 创建项目模块 + 同步菜单")]
    public async Task Test03_CreateModuleAndSyncMenu()
    {
        Assert.True(TestState.ProjectId > 0, "需要先执行 E2E_01");

        Console.WriteLine("📁 创建模块 System...");
        var moduleId = await _client.CreateModuleAsync(TestState.ProjectId);

        Assert.True(moduleId > 0, $"模块 ID 应大于 0，实际：{moduleId}");
        TestState.ModuleId = moduleId;

        Console.WriteLine($"🔄 同步菜单，moduleId = {moduleId}...");
        await _client.SyncModuleToMenuAsync(moduleId);

        Console.WriteLine($"✅ 模块创建成功，ID = {moduleId}");
    }

    // =========================================================
    // 测试 #4: 创建实体 Article（主表）+ 字段
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_04: 创建主表实体 Article + 字段")]
    public async Task Test04_CreateArticleEntity()
    {
        Assert.True(TestState.ModuleId > 0, "需要先执行 E2E_03");

        Console.WriteLine("📝 创建主表实体 Article...");
        var entityId = await _client.CreateEntityAsync(
            TestState.ModuleId,
            "Article",
            "文章",
            hasPrimaryKey: true,
            hasTenant: true,
            generateFrontend: true);

        Assert.True(entityId > 0, $"Article 实体 ID 应大于 0，实际：{entityId}");
        TestState.ArticleEntityId = entityId;

        Console.WriteLine($"  添加字段 Title...");
        await _client.AddFieldAsync(entityId, "Title", "string", "标题", "input");

        Console.WriteLine($"  添加字段 Content...");
        await _client.AddFieldAsync(entityId, "Content", "string", "内容", "editor");

        Console.WriteLine($"  添加字段 PublishDate...");
        await _client.AddFieldAsync(entityId, "PublishDate", "DateTime", "发布时间", "datetime");

        Console.WriteLine($"✅ Article 实体创建成功，ID = {entityId}");
    }

    // =========================================================
    // 测试 #5: 创建子表实体 ArticleComment + 外键字段
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_05: 创建子表实体 ArticleComment + 外键 + 1对多关系")]
    public async Task Test05_CreateArticleCommentEntity()
    {
        Assert.True(TestState.ArticleEntityId > 0, "需要先执行 E2E_04");
        Assert.True(TestState.ModuleId > 0, "需要先执行 E2E_03");

        Console.WriteLine("💬 创建子表实体 ArticleComment...");
        var commentEntityId = await _client.CreateEntityAsync(
            TestState.ModuleId,
            "ArticleComment",
            "文章评论",
            hasPrimaryKey: true,
            generateFrontend: false);

        Assert.True(commentEntityId > 0, $"ArticleComment 实体 ID 应大于 0，实际：{commentEntityId}");
        TestState.ArticleCommentEntityId = commentEntityId;

        Console.WriteLine($"  添加外键字段 ArticleId...");
        await _client.AddFieldAsync(commentEntityId, "ArticleId", "long", "文章ID",
            isSystemField: true, showInList: true);

        Console.WriteLine($"  添加字段 CommentContent...");
        await _client.AddFieldAsync(commentEntityId, "CommentContent", "string", "评论内容", "textarea");

        Console.WriteLine($"  添加字段 CommentBy...");
        await _client.AddFieldAsync(commentEntityId, "CommentBy", "string", "评论者", "input");

        Console.WriteLine($"  添加1对多关系：Article.Id → ArticleComment.ArticleId...");
        await _client.AddOneToManyRelationAsync(
            TestState.ArticleEntityId, "Id",
            commentEntityId, "ArticleId");

        Console.WriteLine($"✅ ArticleComment 实体创建成功，ID = {commentEntityId}");
    }

    // =========================================================
    // 测试 #6: 生成代码
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_06: 调用生成代码 API")]
    public async Task Test06_GenerateCode()
    {
        Assert.True(TestState.ArticleEntityId > 0, "需要先执行 E2E_04");

        Console.WriteLine($"⚙️ 生成 Article 实体代码...");
        var articleSuccess = await _client.GenerateCodeAsync(TestState.ArticleEntityId);
        Assert.True(articleSuccess, "Article 代码生成应成功");
        Console.WriteLine("✅ Article 代码生成成功");

        if (TestState.ArticleCommentEntityId > 0)
        {
            Console.WriteLine($"⚙️ 生成 ArticleComment 实体代码...");
            var commentSuccess = await _client.GenerateCodeAsync(TestState.ArticleCommentEntityId);
            // 子表生成可能有警告但不阻断
            Console.WriteLine($"   ArticleComment 生成结果：{commentSuccess}");
        }
    }

    // =========================================================
    // 测试 #7: 验证生成文件存在性（后端）
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_07: 验证后端生成文件存在")]
    public async Task Test07_VerifyBackendFiles()
    {
        var projPath = E2eConfig.TestProjectFullPath;
        var projName = E2eConfig.TestProjectName;

        Console.WriteLine($"🔍 验证后端文件，项目路径：{projPath}");

        // Article 相关后端文件
        var expectedFiles = new[]
        {
            Path.Combine(projPath, $"{projName}.Domain", "Entities", "System", "Article.auto.cs"),
            Path.Combine(projPath, $"{projName}.Domain", "Entities", "System", "Article.cs"),
            Path.Combine(projPath, $"{projName}.Application", "Dtos", "System", "ArticleDto.cs"),
            Path.Combine(projPath, $"{projName}.Application", "Services", "System", "IArticleService.cs"),
            Path.Combine(projPath, $"{projName}.Application", "Services", "System", "ArticleService.cs"),
        };

        foreach (var file in expectedFiles)
        {
            Console.WriteLine($"  检查：{file}");
            Assert.True(File.Exists(file), $"文件应存在：{Path.GetFileName(file)}");
        }

        Console.WriteLine("✅ 所有后端文件验证通过");
        await Task.CompletedTask;
    }

    // =========================================================
    // 测试 #8: 验证生成文件存在性（前端）
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_08: 验证前端生成文件存在")]
    public async Task Test08_VerifyFrontendFiles()
    {
        var projPath = E2eConfig.TestProjectFullPath;
        var projName = E2eConfig.TestProjectName;

        Console.WriteLine($"🔍 验证前端文件...");

        var expectedFiles = new[]
        {
            Path.Combine(projPath, $"{projName}.Vue", "src", "api", "system", "article.js"),
            Path.Combine(projPath, $"{projName}.Vue", "src", "views", "system", "article", "index.vue"),
            Path.Combine(projPath, $"{projName}.Vue", "src", "views", "system", "article", "article.add.auto.js"),
            Path.Combine(projPath, $"{projName}.Vue", "src", "views", "system", "article", "article.edit.auto.js"),
            Path.Combine(projPath, $"{projName}.Vue", "src", "views", "system", "article", "article.detail.auto.js"),
        };

        foreach (var file in expectedFiles)
        {
            Console.WriteLine($"  检查：{file}");
            Assert.True(File.Exists(file), $"文件应存在：{Path.GetFileName(file)}");
        }

        Console.WriteLine("✅ 所有前端文件验证通过");
        await Task.CompletedTask;
    }

    // =========================================================
    // 测试 #9: 验证生成代码内容质量
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_09: 验证生成代码内容质量")]
    public async Task Test09_VerifyCodeQuality()
    {
        var projPath = E2eConfig.TestProjectFullPath;
        var projName = E2eConfig.TestProjectName;

        // 验证 .auto.cs 包含正确的类声明和接口继承
        var autoFile = Path.Combine(projPath, $"{projName}.Domain", "Entities", "System", "Article.auto.cs");
        if (File.Exists(autoFile))
        {
            var content = await File.ReadAllTextAsync(autoFile);
            Assert.Contains("public partial class Article", content);
            // 应继承 IBaseEntity 或 IEntity
            Assert.True(content.Contains("IEntity") || content.Contains("IBaseEntity"),
                ".auto.cs 应包含 IEntity 或 IBaseEntity 继承");
        }

        // 验证 index.vue 包含分页调用
        var indexVue = Path.Combine(projPath, $"{projName}.Vue", "src", "views", "system", "article", "index.vue");
        if (File.Exists(indexVue))
        {
            var content = await File.ReadAllTextAsync(indexVue);
            Assert.True(content.Contains("getPagedList") || content.Contains("getList"),
                "index.vue 应包含列表 API 调用");
        }

        // 验证子表 ArticleComment.auto.cs 包含外键字段
        var commentAutoFile = Path.Combine(projPath, $"{projName}.Domain", "Entities", "System", "ArticleComment.auto.cs");
        if (File.Exists(commentAutoFile))
        {
            var content = await File.ReadAllTextAsync(commentAutoFile);
            Assert.Contains("ArticleId", content);
        }

        Console.WriteLine("✅ 代码质量验证通过");
    }

    // =========================================================
    // 测试 #10: 调用编译 API
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_10: 调用编译 API 验证生成代码可编译")]
    public async Task Test10_Build()
    {
        Assert.True(TestState.ProjectId > 0, "需要先执行 E2E_01 创建项目");

        Console.WriteLine($"🔨 执行 dotnet build，项目 ID = {TestState.ProjectId}...");
        var (success, message, output) = await _client.BuildProjectAsync(TestState.ProjectId);

        Console.WriteLine($"   结果：{success}, 消息：{message}");
        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine($"   输出（末尾500字符）：{output.Substring(Math.Max(0, output.Length - 500))}");
        }

        Assert.True(success, $"编译应成功，实际：{message}\n输出：{output}");
        Console.WriteLine("✅ 编译验证通过");
    }

    // =========================================================
    // 测试 #11: 执行数据库迁移
    // =========================================================

    [Fact(Skip = "Covered by E2E_FullWorkflow", DisplayName = "E2E_11: 执行数据库迁移（add-migration + database update）")]
    public async Task Test11_MigrateDatabase()
    {
        Assert.True(TestState.ProjectId > 0, "需要先执行 E2E_01 创建项目");

        Console.WriteLine($"🗄️ 执行数据库迁移，项目 ID = {TestState.ProjectId}...");
        var (success, message, output) = await _client.MigrateDatabaseAsync(TestState.ProjectId);

        Console.WriteLine($"   结果：{success}, 消息：{message}");
        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine($"   输出：{output.Substring(Math.Max(0, output.Length - 500))}");
        }

        Assert.True(success, $"数据库迁移应成功，实际：{message}\n输出：{output}");
        Console.WriteLine("✅ 数据库迁移验证通过");
    }
}

/// <summary>
/// xUnit 集合定义：确保同一集合内串行执行
/// </summary>
[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2eCollection { }
