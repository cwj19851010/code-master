using CodeMaster.Infrastructure.Persistence.SqlSugar;
using Microsoft.OpenApi.Models;
using SqlSugar;
using Yitter.IdGenerator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CodeMaster.Core.MultiTenancy;
using CodeMaster.Core.Data;
using CodeMaster.Infrastructure.MultiTenancy;
using CodeMaster.Infrastructure.DynamicApi;
using System.Reflection;
using Mapster;
using CodeMaster.Infrastructure.ModelBinding;
using CodeMaster.Infrastructure.Middleware;
using CodeMaster.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using CodeMaster.Application.Services;
using CodeMaster.WebApi.Extensions;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Domain.Entities.Monitor;
using Microsoft.AspNetCore.SignalR;
using CodeMaster.Infrastructure.Caching.Extensions;
using CodeMaster.Infrastructure.SignalR;
using CodeMaster.Core.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"[Config] Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[Config] ContentRoot: {builder.Environment.ContentRootPath}");
Console.WriteLine($"[Config] appsettings.json exists: {File.Exists(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"))}");
Console.WriteLine($"[Config] appsettings.{builder.Environment.EnvironmentName}.json exists: {File.Exists(Path.Combine(builder.Environment.ContentRootPath, $"appsettings.{builder.Environment.EnvironmentName}.json"))}");

// 配置雪花ID
var workerId = builder.Configuration.GetValue<ushort>("SnowflakeId:WorkerId", (ushort)1);
YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = workerId });

// 注册数据过滤器（Singleton，全局单例）
builder.Services.AddSingleton<IDataFilter, DataFilter>();

// 注册 HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 注册数据权限上下文（Scoped）
builder.Services.AddScoped<IDataPermissionContext, DataPermissionContext>();

// 注册租户上下文（Scoped，每个请求一个实例）
builder.Services.AddScoped<ITenantContext, TenantContext>();

// 自动注册仓储
builder.Services.AddRepositories();

// 获取 Application 程序集
var applicationAssembly = Assembly.Load("CodeMaster.Application");

// 自动注册所有 Application Service
builder.Services.AddApplicationServices(applicationAssembly);

// 手动注册认证相关服务
builder.Services.AddScoped<CodeMaster.Application.Services.Auth.IJwtService, CodeMaster.Application.Services.Auth.JwtService>();
builder.Services.AddScoped<CodeMaster.Application.Services.Auth.IAuthService, CodeMaster.Application.Services.Auth.AuthService>();
builder.Services.AddScoped<CodeMaster.Application.Services.Monitor.IOnlineUserService, CodeMaster.Application.Services.Monitor.OnlineUserService>();

// 注册 Excel 导入导出服务
builder.Services.AddScoped<CodeMaster.Core.Services.IExcelService, CodeMaster.Infrastructure.Services.ExcelService>();

// 注册缓存服务
builder.Services.AddCaching(builder.Configuration);

// 注册动态 API 描述提供器
builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider, CodeMaster.WebApi.Swagger.DynamicApiDescriptionProvider>();

// 添加控制器和动态 API
builder.Services.AddControllersWithViews(options =>
    {
        // 添加查询字符串模型绑定器，支持从Query String绑定复杂对象
        options.ModelBinderProviders.Insert(0, new QueryStringModelBinderProvider());

        // 添加全局操作日志过滤器
        options.Filters.Add<CodeMaster.WebApi.Filters.GlobalOperationLogFilter>();
    })
    .AddJsonOptions(options =>
    {
        // 配置JSON序列化选项
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // 反序列化时不区分大小写
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // 序列化时使用小驼峰命名

        // 添加Long转字符串转换器，解决JavaScript精度丢失问题
        options.JsonSerializerOptions.Converters.Add(new CodeMaster.WebApi.Converters.LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new CodeMaster.WebApi.Converters.NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new CodeMaster.WebApi.Converters.UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new CodeMaster.WebApi.Converters.NullableUtcDateTimeConverter());
    })
    .AddDynamicApi(applicationAssembly, options =>
    {
        options.RoutePrefix = "api";
        options.RemoveServiceSuffix = true;
        options.UseLowercaseRoutes = true;
    });


// 配置Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeMaster API",
        Version = "v1",
        Description = "CodeMaster 代码大师 - 企业级快速开发平台API文档",
        Contact = new OpenApiContact
        {
            Name = "CodeMaster Team",
            Email = "support@codemaster.com"
        }
    });

    // JWT认证配置
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 自定义 Schema ID 生成器，避免类型名称冲突
    options.CustomSchemaIds(type => type.FullName);

    // 解决动态 API 可能导致的操作 ID 冲突
    options.CustomOperationIds(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controllerName}_{actionName}";
    });

    // 解决动态 API 的 HTTP Method 歧义问题
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // XML注释
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// 配置SqlSugar（使用Scoped，支持租户上下文、数据过滤器和数据权限）
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException($"Missing configuration: ConnectionStrings:DefaultConnection. ContentRoot={builder.Environment.ContentRootPath}, Environment={builder.Environment.EnvironmentName}, appsettings.json exists={File.Exists(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"))}");
}

if (string.IsNullOrWhiteSpace(dbProvider))
{
    throw new InvalidOperationException($"Missing configuration: DbProvider. ContentRoot={builder.Environment.ContentRootPath}, Environment={builder.Environment.EnvironmentName}, appsettings.json exists={File.Exists(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"))}");
}

dbProvider = dbProvider.Trim();
Console.WriteLine($"[SqlSugar] 数据库提供程序: {dbProvider}");
var dbType = dbProvider.ToLowerInvariant() switch
{
    "sqlite" => DbType.Sqlite,
    "mysql" => DbType.MySql,
    "oracle" => DbType.Oracle,
    "postgresql" or "postgres" or "pgsql" => DbType.PostgreSQL,
    "sqlserver" => DbType.SqlServer,
    _ => throw new InvalidOperationException($"Unsupported DbProvider: {dbProvider}")
};
Console.WriteLine($"[SqlSugar] DbType: {dbType}");
Console.WriteLine($"[SqlSugar] ConnectionString: {MaskConnectionString(connectionString)}");
builder.Services.AddScoped<ISqlSugarClient>(sp =>
{
    var tenantContext = sp.GetService<ITenantContext>();
    var dataFilter = sp.GetRequiredService<IDataFilter>();
    var dataPermissionContext = sp.GetService<IDataPermissionContext>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("SqlSugar");
    return SqlSugarSetup.CreateSqlSugarClient(connectionString!, tenantContext, dataFilter, dataPermissionContext, dbType, logger);
});

// 注册Repository
builder.Services.AddScoped(typeof(CodeMaster.Core.Repositories.IRepository<>), typeof(CodeMaster.Infrastructure.Persistence.Repositories.Repository<>));
builder.Services.AddScoped(typeof(CodeMaster.Core.Repositories.IReadOnlyRepository<>), typeof(CodeMaster.Infrastructure.Persistence.Repositories.ReadOnlyRepository<>));

// 配置JWT认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
if (!jwtSettings.Exists())
{
    jwtSettings = builder.Configuration.GetSection("Jwt");
}
var secretKey = jwtSettings.GetValue<string>("SecretKey");
var issuer = jwtSettings.GetValue<string>("Issuer");
var audience = jwtSettings.GetValue<string>("Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero
    };

    // SignalR 需要从查询字符串中读取 Token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // 如果是 SignalR Hub 请求，从查询字符串获取 Token
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// 配置授权与权限策略
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// 配置CORS（SignalR 需要支持 Credentials）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowCredentials = builder.Configuration.GetValue<bool>("Cors:AllowCredentials");

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();

            if (allowCredentials)
            {
                policy.AllowCredentials();
            }
        }
        else
        {
            // 无配置时，允许任意源但不允许携带凭据
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// 配置 SignalR + 在线用户管理器
builder.Services.AddSignalRNotification();

// 注册通知服务
builder.Services.AddScoped<CodeMaster.Core.Services.Monitor.INotificationService, CodeMaster.Infrastructure.SignalR.NotificationService>();

// 注册任务调度服务
builder.Services.AddHttpClient(); // HttpJob 需要
builder.Services.AddSingleton<Quartz.Spi.IJobFactory, CodeMaster.Infrastructure.TaskScheduling.JobFactory>();
builder.Services.AddSingleton<CodeMaster.Infrastructure.TaskScheduling.ITaskSchedulerServer, CodeMaster.Infrastructure.TaskScheduling.TaskSchedulerServer>();
builder.Services.AddScoped<CodeMaster.Infrastructure.TaskScheduling.Jobs.HttpJob>();
builder.Services.AddScoped<CodeMaster.Infrastructure.TaskScheduling.Jobs.SqlJob>();
builder.Services.AddScoped<CodeMaster.Infrastructure.TaskScheduling.Jobs.AssemblyJob>();

var app = builder.Build();

// 启动任务调度器
var schedulerServer = app.Services.GetRequiredService<CodeMaster.Infrastructure.TaskScheduling.ITaskSchedulerServer>();
await schedulerServer.StartTaskScheduleAsync();

// 加载所有启用的任务
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    var tasks = await db.Queryable<SysTask>()
        .Where(t => t.Status == 0)
        .ToListAsync();

    foreach (var task in tasks)
    {
        await schedulerServer.AddTaskScheduleAsync(task);
    }

    Console.WriteLine($"已加载 {tasks.Count} 个启用的任务");
}

// 配置HTTP请求管道
var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false);
if (swaggerEnabled)
{
    // 为没有 HttpMethod 特性的 Action 自动设置 HTTP Method（必须在 UseSwagger 之前调用）
    app.AutoHttpMethodIfActionNoBind();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeMaster API V1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "CodeMaster API文档";
    });
}

// 开发环境不使用 HTTPS 重定向
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// 启用静态文件服务（用于文件上传下载）
var webRootPath = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRootPath))
    webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});

// HTTP请求日志中间件
app.UseRequestLogging();

// Authentication must run before tenant resolution so JWT TenantId is available.
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseDataPermission();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 映射 SignalR Hub
app.MapHub<CodeMaster.Infrastructure.SignalR.NotificationHub>("/hubs/notification");


// 启动信息
Console.WriteLine("===========================================");
Console.WriteLine("  CodeMaster API 启动成功！");
Console.WriteLine($"  环境: {app.Environment.EnvironmentName}");
if (swaggerEnabled)
{
    Console.WriteLine($"  Swagger: https://localhost:5001/swagger");
}
else
{
    Console.WriteLine("  Swagger: disabled");
}
Console.WriteLine("===========================================");

app.Run();

static string MaskConnectionString(string connectionString)
{
    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (var i = 0; i < parts.Length; i++)
    {
        var separatorIndex = parts[i].IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = parts[i][..separatorIndex];
        if (key.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("Pwd", StringComparison.OrdinalIgnoreCase))
        {
            parts[i] = $"{key}=***";
        }
    }

    return string.Join(';', parts);
}
