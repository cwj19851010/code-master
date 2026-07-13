using CodeMaster.Core.Enums;
using CodeMaster.Domain.Entities.CodeGen;
using SqlSugar;
using Yitter.IdGenerator;

YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = 1 });

var repoRoot = FindRepositoryRoot();
var databasePath = Environment.GetEnvironmentVariable("CODEMASTER_SEED_DB")
    ?? Path.Combine(repoRoot, "CodeMaster.Migrator", "CodeMaster.db");

var sampleProjectPath = Environment.GetEnvironmentVariable("CODEMASTER_SAMPLE_PROJECT_PATH")
    ?? Path.Combine(Path.GetTempPath(), "CodeMasterSamples", "OrderSystem");

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
Directory.CreateDirectory(sampleProjectPath);

var db = new SqlSugarClient(new ConnectionConfig
{
    ConnectionString = $"Data Source={databasePath}",
    DbType = DbType.Sqlite,
    IsAutoCloseConnection = true,
    ConfigureExternalServices = new ConfigureExternalServices
    {
        EntityNameService = (type, entity) =>
        {
            var attr = type.GetCustomAttributes(typeof(SugarTable), false).FirstOrDefault() as SugarTable;
            if (attr != null && !string.IsNullOrEmpty(attr.TableName))
            {
                entity.DbTableName = attr.TableName;
                return;
            }

            var tableName = type.Name.EndsWith("s", StringComparison.Ordinal) ? type.Name : $"{type.Name}s";
            entity.DbTableName = ToSnakeCase(tableName);
        },
        EntityService = (property, column) => column.DbColumnName = ToSnakeCase(property.Name)
    }
});

Console.WriteLine("Seeding sample CodeMaster project metadata...");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine($"Project path: {sampleProjectPath}");

var projectId = UpsertProject(db, sampleProjectPath);
var moduleId = UpsertModule(db, projectId);

UpsertEntity(db, projectId, moduleId, "Product", "sys_products", "Product", new[]
{
    new FieldDef("Name", "string", 128, true, true, 1, "Product name"),
    new FieldDef("Code", "string", 64, true, true, 2, "Product code"),
    new FieldDef("Price", "decimal", null, false, true, 3, "Price") { Precision = 18, Scale = 2 },
    new FieldDef("Stock", "int", null, false, true, 4, "Stock"),
    new FieldDef("Category", "string", 64, false, true, 5, "Category"),
    new FieldDef("Status", "string", 32, false, true, 10, "Status") { SelectOptions = "OnSale,OffSale,OutOfStock" },
});

UpsertEntity(db, projectId, moduleId, "Order", "sys_orders", "Order", new[]
{
    new FieldDef("OrderNo", "string", 32, true, true, 1, "Order number"),
    new FieldDef("CustomerName", "string", 64, true, true, 2, "Customer name"),
    new FieldDef("CustomerPhone", "string", 20, false, true, 3, "Customer phone"),
    new FieldDef("TotalAmount", "decimal", null, false, true, 4, "Total amount") { Precision = 18, Scale = 2 },
    new FieldDef("Status", "string", 32, false, true, 5, "Status") { SelectOptions = "Pending,Paid,Shipped,Completed,Cancelled" },
    new FieldDef("OrderTime", "DateTime", null, false, true, 6, "Order time"),
    new FieldDef("PayTime", "DateTime", null, false, false, 7, "Pay time"),
    new FieldDef("DeliveryAddress", "string", 256, false, false, 8, "Delivery address"),
});

UpsertEntity(db, projectId, moduleId, "OrderItem", "sys_order_items", "Order item", new[]
{
    new FieldDef("OrderId", "long", null, true, true, 1, "Order ID"),
    new FieldDef("ProductId", "long", null, true, true, 2, "Product ID"),
    new FieldDef("ProductName", "string", 128, false, true, 3, "Product name"),
    new FieldDef("Quantity", "int", null, true, true, 4, "Quantity"),
    new FieldDef("UnitPrice", "decimal", null, false, true, 5, "Unit price") { Precision = 18, Scale = 2 },
    new FieldDef("SubTotal", "decimal", null, false, true, 6, "Sub total") { Precision = 18, Scale = 2 },
});

Console.WriteLine("Done.");

static long UpsertProject(SqlSugarClient db, string sampleProjectPath)
{
    var existing = db.Queryable<Project>().First(p => p.ProjectName == "OrderSystem");
    if (existing != null)
    {
        Console.WriteLine($"Project exists: {existing.ProjectName} ({existing.Id})");
        return existing.Id;
    }

    var id = YitIdHelper.NextId();
    db.Insertable(new Project
    {
        Id = id,
        ProjectName = "OrderSystem",
        DisplayName = "Order System",
        DisplayNameEn = "Order System",
        Description = "Sample order management project",
        DescriptionEn = "Sample order management project",
        DatabaseType = DatabaseType.SQLite,
        ConnectionString = "Data Source=OrderSystem.db",
        ProjectPath = sampleProjectPath,
        ProjectType = ProjectType.Server,
        Status = ProjectStatus.NotInitialized,
        FrontendPort = 5173,
        BackendPort = 5000,
        CreateTime = DateTime.UtcNow
    }).ExecuteCommand();

    Console.WriteLine($"Created project: OrderSystem ({id})");
    return id;
}

static long UpsertModule(SqlSugarClient db, long projectId)
{
    var existing = db.Queryable<ProjectModule>()
        .First(m => m.ProjectId == projectId && m.ModuleName == "order");

    if (existing != null)
    {
        Console.WriteLine($"Module exists: {existing.ModuleName} ({existing.Id})");
        return existing.Id;
    }

    var id = YitIdHelper.NextId();
    db.Insertable(new ProjectModule
    {
        Id = id,
        ProjectId = projectId,
        ModuleName = "order",
        ModuleDescription = "Order Management",
        Icon = "Document",
        OrderNum = 1,
        RoutePath = "/order",
        CreateTime = DateTime.UtcNow
    }).ExecuteCommand();

    Console.WriteLine($"Created module: order ({id})");
    return id;
}

static void UpsertEntity(
    SqlSugarClient db,
    long projectId,
    long moduleId,
    string name,
    string tableName,
    string description,
    IEnumerable<FieldDef> fields)
{
    var existing = db.Queryable<ModuleEntity>()
        .First(e => e.ModuleId == moduleId && e.Name == name);

    var entityId = existing?.Id ?? YitIdHelper.NextId();
    if (existing == null)
    {
        db.Insertable(new ModuleEntity
        {
            Id = entityId,
            ProjectId = projectId,
            ModuleId = moduleId,
            Name = name,
            TableName = tableName,
            Description = description,
            HasPrimaryKey = true,
            HasAudit = true,
            HasSoftDelete = true,
            GenerateFrontend = true,
            OrderNum = 1,
            CreateTime = DateTime.UtcNow
        }).ExecuteCommand();

        Console.WriteLine($"Created entity: {name} ({entityId})");
    }
    else
    {
        Console.WriteLine($"Entity exists: {name} ({entityId})");
    }

    var existingFields = new HashSet<string>(db.Queryable<EntityField>()
        .Where(f => f.ModuleEntityId == entityId)
        .Select(f => f.Name)
        .ToList(), StringComparer.Ordinal);

    foreach (var field in fields)
    {
        if (existingFields.Contains(field.Name))
        {
            continue;
        }

        db.Insertable(CreateField(entityId, field)).ExecuteCommand();
        Console.WriteLine($"  Added field: {field.Name}");
    }
}

static EntityField CreateField(long entityId, FieldDef field)
{
    return new EntityField
    {
        Id = YitIdHelper.NextId(),
        ModuleEntityId = entityId,
        Name = field.Name,
        Description = field.Description,
        DataType = field.DataType,
        MaxLength = field.MaxLength,
        Precision = field.Precision,
        Scale = field.Scale,
        IsRequired = field.Required,
        IsNullable = !field.Required,
        ShowInList = field.ShowInList,
        ShowInDetail = true,
        ShowInAddForm = true,
        ShowInEditForm = true,
        ShowInSearch = field.ShowInList,
        OrderNum = field.Order,
        SelectOptions = field.SelectOptions,
        FormControlType = field.SelectOptions != null
            ? "select"
            : field.DataType switch
            {
                "decimal" or "int" or "long" => "number",
                "DateTime" => "datetime",
                _ => "input"
            },
        CreateTime = DateTime.UtcNow
    };
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "CodeMaster.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}

static string ToSnakeCase(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    var chars = new List<char>(value.Length + 8) { char.ToLowerInvariant(value[0]) };
    for (var i = 1; i < value.Length; i++)
    {
        var current = value[i];
        if (char.IsUpper(current))
        {
            chars.Add('_');
            chars.Add(char.ToLowerInvariant(current));
        }
        else
        {
            chars.Add(current);
        }
    }

    return new string(chars.ToArray());
}

record FieldDef(
    string Name,
    string DataType,
    int? MaxLength,
    bool Required,
    bool ShowInList,
    int Order,
    string Description)
{
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public string? SelectOptions { get; init; }
}
