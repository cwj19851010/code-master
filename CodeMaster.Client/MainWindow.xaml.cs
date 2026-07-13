using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using Newtonsoft.Json;
using SqlSugar;

namespace CodeMaster.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async(null);

            // 注册 JSBridge
            WebView.CoreWebView2.AddHostObjectToScript("jsbridge", new JsBridge());

            // 监听导航完成事件
            WebView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                StatusTextBlock.Text = $"已加载: {WebView.Source}";
            };

            StatusTextBlock.Text = "WebView2 初始化成功";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavigateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(UrlTextBox.Text))
        {
            WebView.Source = new Uri(UrlTextBox.Text);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Reload();
    }

    private void DevToolsButton_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.OpenDevToolsWindow();
        }
    }
}

/// <summary>
/// JSBridge - 供前端 JavaScript 调用的接口
/// </summary>
[System.Runtime.InteropServices.ComVisible(true)]
public class JsBridge
{
    /// <summary>
    /// 选择文件夹
    /// </summary>
    public string SelectFolder()
    {
        string selectedPath = string.Empty;

        // 使用 Dispatcher 在 UI 线程上执行
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "选择项目目录"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedPath = dialog.FolderName;
            }
        });

        return selectedPath;
    }

    /// <summary>
    /// 显示消息框
    /// </summary>
    public void ShowMessage(string message, string title)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    /// <summary>
    /// 同步模块到菜单（客户端模式）
    /// </summary>
    public string SyncModuleToMenu(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<SyncModuleToMenuData>(jsonData);
            if (data == null)
            {
                return JsonConvert.SerializeObject(new { success = false, message = "Invalid JSON data" });
            }

            // 1. 读取项目配置获取连接字符串
            var appsettingsPath = Path.Combine(data.ProjectPath, $"{data.ProjectName}.WebApi", "appsettings.json");
            if (!File.Exists(appsettingsPath))
            {
                return JsonConvert.SerializeObject(new { success = false, message = $"配置文件不存在: {appsettingsPath}" });
            }

            var configJson = File.ReadAllText(appsettingsPath);
            dynamic config = JsonConvert.DeserializeObject(configJson)!;
            string connectionString = config.ConnectionStrings.DefaultConnection;
            string databaseType = config.DatabaseType;

            // 2. 创建数据库连接
            var dbType = ParseDbType(databaseType);
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true
            });

            // 3. 检查菜单是否已存在
            var existingMenu = db.Queryable<dynamic>()
                .AS("sys_menu")
                .Where("lang_key = @langKey", new { langKey = data.ModuleName })
                .First();

            if (existingMenu != null)
            {
                // 更新菜单
                db.Ado.ExecuteCommand(@"
                    UPDATE sys_menu
                    SET menu_name = @menuName, icon = @icon, order_num = @orderNum, update_time = @updateTime
                    WHERE lang_key = @langKey",
                    new
                    {
                        menuName = data.ModuleDescription,
                        icon = data.Icon ?? "folder",
                        orderNum = data.OrderNum,
                        updateTime = DateTime.UtcNow,
                        langKey = data.ModuleName
                    });
            }
            else
            {
                // 插入新菜单
                var menuId = DateTime.UtcNow.Ticks;
                db.Ado.ExecuteCommand(@"
                    INSERT INTO sys_menu (id, menu_name, lang_key, parent_id, order_num, path, component, menu_type, visible, status, icon, create_time, is_deleted)
                    VALUES (@id, @menuName, @langKey, @parentId, @orderNum, @path, @component, @menuType, @visible, @status, @icon, @createTime, @isDeleted)",
                    new
                    {
                        id = menuId,
                        menuName = data.ModuleDescription,
                        langKey = data.ModuleName,
                        parentId = 0L,
                        orderNum = data.OrderNum,
                        path = $"/{data.ModuleName}",
                        component = "",
                        menuType = 0,
                        visible = true,
                        status = true,
                        icon = data.Icon ?? "folder",
                        createTime = DateTime.UtcNow,
                        isDeleted = false
                    });
            }

            // 4. 同步国际化
            SyncLanguage(db, data.ModuleName, data.ModuleDescription);

            return JsonConvert.SerializeObject(new { success = true, message = "同步成功" });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
        }
    }

    private void SyncLanguage(SqlSugarClient db, string langKey, string description)
    {
        // 同步中文翻译
        var existingZh = db.Queryable<dynamic>()
            .AS("sys_lang_text")
            .Where("lang_key = @langKey AND lang_code = @langCode", new { langKey, langCode = "zh-CN" })
            .First();

        if (existingZh != null)
        {
            db.Ado.ExecuteCommand(@"
                UPDATE sys_lang_text
                SET text = @text, update_time = @updateTime
                WHERE lang_key = @langKey AND lang_code = @langCode",
                new
                {
                    text = description,
                    updateTime = DateTime.UtcNow,
                    langKey,
                    langCode = "zh-CN"
                });
        }
        else
        {
            db.Ado.ExecuteCommand(@"
                INSERT INTO sys_lang_text (id, lang_key, lang_code, text, create_time, is_deleted)
                VALUES (@id, @langKey, @langCode, @text, @createTime, @isDeleted)",
                new
                {
                    id = DateTime.UtcNow.Ticks,
                    langKey,
                    langCode = "zh-CN",
                    text = description,
                    createTime = DateTime.UtcNow,
                    isDeleted = false
                });
        }

        // 同步英文翻译
        var existingEn = db.Queryable<dynamic>()
            .AS("sys_lang_text")
            .Where("lang_key = @langKey AND lang_code = @langCode", new { langKey, langCode = "en-US" })
            .First();

        if (existingEn != null)
        {
            db.Ado.ExecuteCommand(@"
                UPDATE sys_lang_text
                SET text = @text, update_time = @updateTime
                WHERE lang_key = @langKey AND lang_code = @langCode",
                new
                {
                    text = langKey,
                    updateTime = DateTime.UtcNow,
                    langKey,
                    langCode = "en-US"
                });
        }
        else
        {
            db.Ado.ExecuteCommand(@"
                INSERT INTO sys_lang_text (id, lang_key, lang_code, text, create_time, is_deleted)
                VALUES (@id, @langKey, @langCode, @text, @createTime, @isDeleted)",
                new
                {
                    id = DateTime.UtcNow.Ticks + 1,
                    langKey,
                    langCode = "en-US",
                    text = langKey,
                    createTime = DateTime.UtcNow,
                    isDeleted = false
                });
        }
    }

    private DbType ParseDbType(string databaseType)
    {
        return databaseType switch
        {
            "MySql" => DbType.MySql,
            "SqlServer" => DbType.SqlServer,
            "PostgreSQL" => DbType.PostgreSQL,
            "Sqlite" => DbType.Sqlite,
            "Oracle" => DbType.Oracle,
            _ => throw new NotSupportedException($"不支持的数据库类型: {databaseType}")
        };
    }

    /// <summary>
    /// 初始化项目（从 Base64 模板）
    /// </summary>
    public string InitializeProject(string jsonData)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<InitializeProjectData>(jsonData);
            if (data == null)
            {
                return JsonConvert.SerializeObject(new { success = false, message = "Invalid JSON data" });
            }

            // 1. 解码 Base64 为字节数组
            var zipBytes = Convert.FromBase64String(data.TemplateBase64);

            // 2. 创建项目目录
            if (Directory.Exists(data.ProjectPath))
            {
                return JsonConvert.SerializeObject(new { success = false, message = $"目录已存在: {data.ProjectPath}" });
            }
            Directory.CreateDirectory(data.ProjectPath);

            // 3. 解压模板
            using (var memoryStream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(data.ProjectPath);
            }

            // 4. 重命名文件和目录
            RenameFilesAndDirectories(data.ProjectPath, data.ProjectName);

            // 5. 替换项目名称
            ReplaceProjectName(data.ProjectPath, data.ProjectName);

            // 6. 生成 .sln 文件
            GenerateSolutionFile(data.ProjectPath, data.ProjectName);

            // 7. 更新数据库配置
            UpdateDatabaseConfig(data.ProjectPath, data.ProjectName, data.DatabaseType, data.ConnectionString);

            // 8. 运行数据库迁移
            var migrationResult = RunDatabaseMigration(data.ProjectPath, data.ProjectName);
            if (!migrationResult.Success)
            {
                return JsonConvert.SerializeObject(new { success = false, message = $"数据库迁移失败: {migrationResult.Message}" });
            }

            // 9. 运行 npm install
            var npmResult = RunNpmInstall(data.ProjectPath, data.ProjectName);
            if (!npmResult.Success)
            {
                return JsonConvert.SerializeObject(new { success = false, message = $"npm install 失败: {npmResult.Message}" });
            }

            return JsonConvert.SerializeObject(new { success = true, message = "项目初始化成功" });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
        }
    }

    private void RenameFilesAndDirectories(string projectPath, string newProjectName)
    {
        // 重命名目录
        var directories = Directory.GetDirectories(projectPath, "CodeMaster.*", SearchOption.AllDirectories);
        foreach (var dir in directories.OrderByDescending(d => d.Length))
        {
            var dirName = Path.GetFileName(dir);
            var newDirName = dirName.Replace("CodeMaster", newProjectName);
            var newDirPath = Path.Combine(Path.GetDirectoryName(dir)!, newDirName);
            Directory.Move(dir, newDirPath);
        }

        // 重命名文件
        var files = Directory.GetFiles(projectPath, "CodeMaster.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var newFileName = fileName.Replace("CodeMaster", newProjectName);
            var newFilePath = Path.Combine(Path.GetDirectoryName(file)!, newFileName);
            File.Move(file, newFilePath);
        }
    }

    private void ReplaceProjectName(string projectPath, string newProjectName)
    {
        var fileExtensions = new[] { ".cs", ".csproj", ".json", ".js", ".vue", ".ts", ".tsx", ".html" };
        var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => fileExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var newContent = content.Replace("CodeMaster", newProjectName);
            if (content != newContent)
            {
                File.WriteAllText(file, newContent);
            }
        }
    }

    private void GenerateSolutionFile(string projectPath, string projectName)
    {
        var slnFile = Path.Combine(projectPath, $"{projectName}.sln");
        var templateSlnFile = Path.Combine(projectPath, "CodeMaster.sln");

        if (File.Exists(templateSlnFile))
        {
            var content = File.ReadAllText(templateSlnFile);
            var newContent = content.Replace("CodeMaster", projectName);
            File.WriteAllText(slnFile, newContent);
            File.Delete(templateSlnFile);
        }
    }

    private void UpdateDatabaseConfig(string projectPath, string projectName, string databaseType, string connectionString)
    {
        var appsettingsFile = Path.Combine(projectPath, $"{projectName}.WebApi", "appsettings.json");
        if (File.Exists(appsettingsFile))
        {
            var json = File.ReadAllText(appsettingsFile);
            dynamic config = JsonConvert.DeserializeObject(json)!;
            config.ConnectionStrings.DefaultConnection = connectionString;
            config.DatabaseType = databaseType;
            File.WriteAllText(appsettingsFile, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }

    private (bool Success, string Message) RunDatabaseMigration(string projectPath, string projectName)
    {
        try
        {
            var migratorPath = Path.Combine(projectPath, $"{projectName}.Migrator");
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run",
                WorkingDirectory = migratorPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return (false, "无法启动 dotnet 进程");
            }

            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return (false, $"Exit code: {process.ExitCode}\n{error}");
            }

            return (true, "数据库迁移成功");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool Success, string Message) RunNpmInstall(string projectPath, string projectName)
    {
        try
        {
            var vuePath = Path.Combine(projectPath, $"{projectName}.Vue");
            if (!Directory.Exists(vuePath))
            {
                return (false, $"Vue 项目目录不存在: {vuePath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c npm install",
                WorkingDirectory = vuePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return (false, "无法启动 npm 进程");
            }

            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return (false, $"Exit code: {process.ExitCode}\n{error}");
            }

            return (true, "npm install 成功");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

/// <summary>
/// 初始化项目数据
/// </summary>
public class InitializeProjectData
{
    public long Id { get; set; }
    public string TemplateBase64 { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// 同步模块到菜单数据
/// </summary>
public class SyncModuleToMenuData
{
    public long ModuleId { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int OrderNum { get; set; }
}
