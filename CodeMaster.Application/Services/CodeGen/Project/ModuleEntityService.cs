using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services;
using CodeMaster.Core.Entities;
using CodeMaster.Core.MultiTenancy;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using CodeMaster.Infrastructure.VueParser;
using CodeMaster.Infrastructure.VueParser.Model;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text;
using CodeMaster.Application.Services.CodeGen.Marker;
using System.Text.RegularExpressions;
using Yitter.IdGenerator;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 模块实体服务
/// </summary>
public class ModuleEntityService : CrudApplicationService<ModuleEntity, ModuleEntityDto, ModuleEntityDto, ModuleEntityQueryDto, CreateModuleEntityDto, UpdateModuleEntityDto>, IModuleEntityService
{
    private readonly ISqlSugarClient _db;
    private readonly ITenantContext? _tenantContext;

    public ModuleEntityService(
        IRepository<ModuleEntity> repository,
        IExcelService excelService,
        ISqlSugarClient db,
        ITenantContext? tenantContext = null,
        CodeMaster.Core.Services.ICacheService? cacheService = null) : base(repository, excelService, cacheService)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// 根据模块ID获取实体列表
    /// </summary>
    public async Task<List<ModuleEntityDto>> GetByModuleIdAsync(long moduleId)
    {
        var entities = await _db.Queryable<ModuleEntity>()
            .Where(e => e.ModuleId == moduleId)
            .OrderBy(e => e.OrderNum)
            .ToListAsync();

        return entities.Adapt<List<ModuleEntityDto>>();
    }

    public async Task<List<ReferenceEntityDto>> GetReferenceEntitiesAsync([FromQuery] long? projectId = null)
    {
        var query = _db.Queryable<ModuleEntity>()
            .Where(e => e.IsDeleted == false);

        if (projectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == projectId.Value);
        }

        var entities = await query
            .OrderBy(e => e.OrderNum)
            .ToListAsync();

        var entityIds = entities.Select(e => e.Id).ToList();
        var fields = entityIds.Count == 0
            ? new List<EntityField>()
            : await _db.Queryable<EntityField>()
                .Where(f => entityIds.Contains(f.ModuleEntityId) && f.IsDeleted == false)
                .OrderBy(f => f.OrderNum)
                .ToListAsync();

        var result = entities.Select(entity =>
        {
            var entityFields = fields
                .Where(f => f.ModuleEntityId == entity.Id)
                .Adapt<List<EntityFieldDto>>();

            var valueField = entityFields.FirstOrDefault(f => f.IsPrimaryKey)?.Name ?? "Id";
            var displayFields = entityFields
                .Where(f => f.DataType == "string" && !f.IsPrimaryKey)
                .OrderBy(f => f.OrderNum)
                .Take(2)
                .Select(f => f.Name)
                .ToList();

            return new ReferenceEntityDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                IsBuiltin = false,
                IsTree = entity.IsTree,
                ValueField = valueField,
                DisplayFields = displayFields,
                Fields = entityFields
            };
        }).ToList();

        result.AddRange(SystemReferenceEntityCatalog.GetAll().Select(SystemReferenceEntityCatalog.ToDto));

        return result;
    }

    /// <summary>
    /// 获取实体详情（包含字段列表和一对多关系）
    /// </summary>
    public override async Task<ModuleEntityDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();

        if (entity == null)
        {
            throw new Exception($"实体不存在: {id}");
        }

        var dto = entity.Adapt<ModuleEntityDto>();

        // 加载字段列表
        var fields = await _db.Queryable<EntityField>()
            .Where(f => f.ModuleEntityId == id)
            .OrderBy(f => f.OrderNum)
            .ToListAsync();

        dto.Fields = fields.Adapt<List<EntityFieldDto>>();

        // 加载一对多关系列表（兼容旧数据库：可能尚未创建该表）
        if (_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
        {
            var relations = await _db.Queryable<OneToManyRelation>()
                .Where(r => r.ModuleEntityId == id)
                .OrderBy(r => r.OrderNum)
                .ToListAsync();

            dto.OneToManyRelations = relations.Adapt<List<OneToManyRelationDto>>();
        }
        else
        {
            dto.OneToManyRelations = new List<OneToManyRelationDto>();
        }

        return dto;
    }

    /// <summary>
    /// 创建实体（包含字段）
    /// </summary>
    public override async Task<long> CreateAsync(CreateModuleEntityDto input)
    {
        try
        {
            _db.Ado.BeginTran();

            // 创建实体
            var entity = input.Adapt<ModuleEntity>();
            FillTenantInfo(entity);
            entity.CreateTime = DateTime.UtcNow;
            entity.Id = await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();

            // 创建字段
            if (input.Fields != null && input.Fields.Count > 0)
            {
                foreach (var fieldDto in input.Fields)
                {
                    var field = fieldDto.Adapt<EntityField>();
                    field.ModuleEntityId = entity.Id;
                    FillTenantInfo(field);
                    field.CreateTime = DateTime.UtcNow;
                    await _db.Insertable(field).ExecuteReturnSnowflakeIdAsync();
                }
            }

            // 创建一对多关系
            if (input.OneToManyRelations != null && input.OneToManyRelations.Count > 0)
            {
                foreach (var relationDto in input.OneToManyRelations)
                {
                    var relation = relationDto.Adapt<OneToManyRelation>();
                    relation.ModuleEntityId = entity.Id;
                    FillTenantInfo(relation);
                    relation.CreateTime = DateTime.UtcNow;
                    await _db.Insertable(relation).ExecuteReturnSnowflakeIdAsync();

                    // 标记子表为被引用状态
                    await MarkChildEntityAsReferenced(relation.ChildEntityId);
                }
            }

            _db.Ado.CommitTran();

            return entity.Id;
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 更新实体（包含字段变更）
    /// </summary>
    public override async Task<int> UpdateAsync(long id, UpdateModuleEntityDto input)
    {
        try
        {
            _db.Ado.BeginTran();

            // 更新实体基本信息
            var entity = await _db.Queryable<ModuleEntity>()
                .Where(e => e.Id == id)
                .FirstAsync();

            if (entity == null)
            {
                throw new Exception($"实体不存在: {id}");
            }

            input.Adapt(entity);
            entity.UpdateTime = DateTime.UtcNow;
            await _db.Updateable(entity).ExecuteCommandAsync();

            // 处理新增字段
            if (input.NewFields != null && input.NewFields.Count > 0)
            {
                var newFields = input.NewFields.Select(f =>
                {
                    var field = f.Adapt<EntityField>();
                    field.ModuleEntityId = id;
                    if (field.Id == 0)
                    {
                        field.Id = YitIdHelper.NextId();
                    }
                    field.CreateBy ??= "system";
                    field.CreateTime = DateTime.UtcNow;
                    field.UpdateUserId ??= 0;
                    FillTenantInfo(field);
                    return field;
                }).ToList();

                await _db.Insertable(newFields).ExecuteCommandAsync();
            }

            // 处理更新字段
            if (input.UpdatedFields != null && input.UpdatedFields.Count > 0)
            {
                foreach (var fieldDto in input.UpdatedFields)
                {
                    var field = await _db.Queryable<EntityField>()
                        .Where(f => f.Id == fieldDto.Id)
                        .FirstAsync();

                    if (field != null)
                    {
                        fieldDto.Adapt(field);
                        field.UpdateTime = DateTime.UtcNow;
                        await _db.Updateable(field).ExecuteCommandAsync();
                    }
                }
            }

            // 处理删除字段
            if (input.DeletedFieldIds != null && input.DeletedFieldIds.Count > 0)
            {
                await _db.Updateable<EntityField>()
                    .SetColumns(f => f.IsDeleted == true)
                    .SetColumns(f => f.DeleteTime == DateTime.UtcNow)
                    .Where(f => f.ModuleEntityId == id && input.DeletedFieldIds.Contains(f.Id))
                    .ExecuteCommandAsync();
            }

            // 处理一对多关系 - 新增
            if (input.NewRelations != null && input.NewRelations.Count > 0)
            {
                foreach (var relationDto in input.NewRelations)
                {
                    var relation = relationDto.Adapt<OneToManyRelation>();
                    relation.ModuleEntityId = id;
                    FillTenantInfo(relation);
                    relation.CreateTime = DateTime.UtcNow;
                    await _db.Insertable(relation).ExecuteReturnSnowflakeIdAsync();
                    await MarkChildEntityAsReferenced(relation.ChildEntityId);
                }
            }

            // 处理一对多关系 - 更新
            if (input.UpdatedRelations != null && input.UpdatedRelations.Count > 0)
            {
                foreach (var relationDto in input.UpdatedRelations)
                {
                    var relation = await _db.Queryable<OneToManyRelation>()
                        .Where(r => r.Id == relationDto.Id)
                        .FirstAsync();

                    if (relation != null)
                    {
                        relationDto.Adapt(relation);
                        relation.UpdateTime = DateTime.UtcNow;
                        await _db.Updateable(relation).ExecuteCommandAsync();
                    }
                }
            }

            // 处理一对多关系 - 删除
            if (input.DeletedRelationIds != null && input.DeletedRelationIds.Count > 0)
            {
                await _db.Updateable<OneToManyRelation>()
                    .SetColumns(r => r.IsDeleted == true)
                    .SetColumns(r => r.DeleteTime == DateTime.UtcNow)
                    .Where(r => r.ModuleEntityId == id && input.DeletedRelationIds.Contains(r.Id))
                    .ExecuteCommandAsync();
            }

            _db.Ado.CommitTran();

            return 1;
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 创建过滤查询
    /// </summary>
    protected override async Task<ISugarQueryable<ModuleEntity>> CreateFilteredQueryAsync(ModuleEntityQueryDto input)
    {
        var query = (ISugarQueryable<ModuleEntity>)Repository.GetQueryable();

        if (input.ProjectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == input.ProjectId.Value);
        }

        if (input.ModuleId.HasValue)
        {
            query = query.Where(e => e.ModuleId == input.ModuleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            query = query.Where(e => e.Name.Contains(input.Name));
        }

        if (!string.IsNullOrWhiteSpace(input.Description))
        {
            query = query.Where(e => e.Description.Contains(input.Description));
        }

        if (input.IsTree.HasValue)
        {
            query = query.Where(e => e.IsTree == input.IsTree.Value);
        }

        if (input.IsReadOnly.HasValue)
        {
            query = query.Where(e => e.IsReadOnly == input.IsReadOnly.Value);
        }

        if (input.IsGenerated.HasValue)
        {
            query = query.Where(e => e.IsGenerated == input.IsGenerated.Value);
        }

        return await Task.FromResult(query);
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    protected override ISugarQueryable<ModuleEntity> ApplySorting(ISugarQueryable<ModuleEntity> queryable, ModuleEntityQueryDto input)
    {
        return queryable.OrderBy(e => e.OrderNum);
    }

    /// <summary>
    /// 同步菜单到目标项目数据库
    /// </summary>
    public async Task<bool> SyncMenuToTargetAsync(long id)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();

        if (entity == null)
        {
            throw new Exception($"实体不存在: {id}");
        }

        var module = await _db.Queryable<ProjectModule>()
            .Where(m => m.Id == entity.ModuleId)
            .FirstAsync();

        if (module == null)
        {
            throw new Exception($"模块不存在: {entity.ModuleId}");
        }

        var project = await _db.Queryable<Project>()
            .Where(p => p.Id == module.ProjectId)
            .FirstAsync();

        if (project == null)
        {
            throw new Exception($"项目不存在: {module.ProjectId}");
        }

        var targetDb = GetTargetDbClient(project);
        await SyncMenuAndPermissionsAsync(entity, module, targetDb);

        return true;
    }

    /// <summary>
    /// 同步字段多语言到目标项目数据库
    /// </summary>
    public async Task<bool> SyncLanguageToTargetAsync(long id)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception($"实体不存在: {id}");

        var module = await _db.Queryable<ProjectModule>()
            .Where(m => m.Id == entity.ModuleId)
            .FirstAsync();

        if (module == null)
            throw new Exception($"模块不存在: {entity.ModuleId}");

        var project = await _db.Queryable<Project>()
            .Where(p => p.Id == module.ProjectId)
            .FirstAsync();

        if (project == null)
            throw new Exception($"项目不存在: {module.ProjectId}");

        var targetDb = GetTargetDbClient(project);

        await SyncModuleLanguageAsync(module, targetDb);
        await SyncEntityAndFieldLanguageAsync(entity, targetDb);

        if (_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
        {
            var relations = await _db.Queryable<OneToManyRelation>()
                .Where(r => r.ModuleEntityId == id)
                .ToListAsync();

            foreach (var relation in relations)
            {
                var childEntity = await _db.Queryable<ModuleEntity>()
                    .Where(e => e.Id == relation.ChildEntityId)
                    .FirstAsync();
                if (childEntity == null) continue;

                await SyncEntityAndFieldLanguageAsync(childEntity, targetDb);
            }
        }

        return true;
    }

    /// <summary>
    /// 生成代码
    /// </summary>
    public async Task<bool> GenerateCodeAsync(long id)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();

        if (entity == null)
        {
            throw new Exception($"实体不存在: {id}");
        }

        // 先递归生成所有一对多子表的代码
        var childRelations = _db.DbMaintenance.IsAnyTable("sys_one_to_many_relation")
            ? await _db.Queryable<OneToManyRelation>().Where(r => r.ModuleEntityId == id).ToListAsync()
            : new List<OneToManyRelation>();

        foreach (var relation in childRelations)
        {
            await GenerateCodeCoreAsync(relation.ChildEntityId);
        }

        await GenerateCodeCoreAsync(id, treeOnly: false);
        return true;
    }

    /// <summary>
    /// 增量生成代码
    /// </summary>
    public async Task<bool> GenerateIncrementalCodeAsync(long id)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();

        if (entity == null)
        {
            throw new Exception($"实体不存在: {id}");
        }

        var childRelations = _db.DbMaintenance.IsAnyTable("sys_one_to_many_relation")
            ? await _db.Queryable<OneToManyRelation>().Where(r => r.ModuleEntityId == id).ToListAsync()
            : new List<OneToManyRelation>();

        foreach (var relation in childRelations)
        {
            await GenerateCodeCoreAsync(relation.ChildEntityId, treeOnly: false, incremental: true);
        }

        await GenerateCodeCoreAsync(id, treeOnly: false, incremental: true);
        return true;
    }

    /// <summary>
    /// 生成树JSON结构（不生成代码文件）
    /// </summary>
    public async Task<bool> GenerateTreeOnlyAsync(long id)
    {
        await GenerateCodeCoreAsync(id, treeOnly: true);
        return true;
    }

    private async Task GenerateCodeCoreAsync(long id, bool treeOnly = false, bool incremental = false)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == id)
            .FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");

        // 获取项目和模块信息
        var module = await _db.Queryable<ProjectModule>()
            .Where(m => m.Id == entity.ModuleId)
            .FirstAsync();

        if (module == null)
        {
            throw new Exception($"模块不存在: {entity.ModuleId}");
        }

        var project = await _db.Queryable<Project>()
            .Where(p => p.Id == module.ProjectId)
            .FirstAsync();

        if (project == null)
        {
            throw new Exception($"项目不存在: {module.ProjectId}");
        }

        // 如果项目路径为空，使用当前目录的父目录
        var projectPath = ResolveProjectRootPath(project);
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        }

        // 创建代码生成器
        var generator = new CodeGeneratorService(_db);
        var projectName = project.ProjectName;

        // 生成后端代码
        var entityAutoCode = await generator.GenerateEntityAutoAsync(id, projectName, module.ModuleName);
        var entityCode = await generator.GenerateEntityAsync(id, projectName, module.ModuleName);
        var dtoCode = await generator.GenerateDtoAsync(id, projectName, module.ModuleName);
        var serviceInterfaceCode = await generator.GenerateServiceInterfaceAsync(id, projectName, module.ModuleName);
        var serviceImplCode = await generator.GenerateServiceImplementationAsync(id, projectName, module.ModuleName);

        // 后端文件路径
        var domainPath = Path.Combine(projectPath, $"{projectName}.Domain", "Entities", module.ModuleName);
        var dtoPath = Path.Combine(projectPath, $"{projectName}.Application", "Dtos", module.ModuleName);
        var servicePath = Path.Combine(projectPath, $"{projectName}.Application", "Services", module.ModuleName);

        // 创建目录
        Directory.CreateDirectory(domainPath);
        Directory.CreateDirectory(dtoPath);
        Directory.CreateDirectory(servicePath);

        // 写入后端文件 — .auto.cs 每次覆盖，.cs 仅首次创建
        await File.WriteAllTextAsync(Path.Combine(domainPath, $"{entity.Name}.auto.cs"), entityAutoCode);

        var entityFilePath = Path.Combine(domainPath, $"{entity.Name}.cs");
        if (!File.Exists(entityFilePath))
        {
            await File.WriteAllTextAsync(entityFilePath, entityCode);
        }

        await File.WriteAllTextAsync(Path.Combine(dtoPath, $"{entity.Name}Dto.cs"), dtoCode);
        await File.WriteAllTextAsync(Path.Combine(servicePath, $"I{entity.Name}Service.cs"), serviceInterfaceCode);
        await File.WriteAllTextAsync(Path.Combine(servicePath, $"{entity.Name}Service.cs"), serviceImplCode);

        // 生成前端 API 文件（子表也需要）
        if (entity.GenerateFrontend)
        {
            var entityNameLower = entity.Name.ToLower();
            var vuePath = Path.Combine(projectPath, $"{projectName}.Vue", "src");
            var apiPath = Path.Combine(vuePath, "api", module.ModuleName.ToLower());

            Directory.CreateDirectory(apiPath);
            var apiCode = await generator.GenerateFrontendApiAsync(id, projectName, module.ModuleName);
            await File.WriteAllTextAsync(Path.Combine(apiPath, $"{entityNameLower}.js"), apiCode);

            // 子表不生成前端页面
            if (!entity.IsChildTable)
            {
                var viewPath = Path.Combine(vuePath, "views", module.ModuleName.ToLower(), entityNameLower);
                Directory.CreateDirectory(viewPath);

                // split mode: 逐页配对（auto.js→PendingSplitExports→.vue→auto.js文件写入）
                // Template-based generation path
                if (CodeGeneratorService.UseTemplateGenerator)
                {
                    var tplGen = new TemplateCodeGenerator(_db);
                    var incrementalChanges = incremental
                        ? await BuildIncrementalChangeSetAsync(entity)
                        : IncrementalChangeSet.Disabled;
                    foreach (var pt in new[] { "index", "add", "edit", "detail" })
                    {
                        var result = await tplGen.GeneratePageAsync(id, pt, projectName, module.ModuleName);
                        var vuePath2 = Path.Combine(viewPath, $"{pt}.vue");
                        var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
                        var finalTemplateHtml = result.VueContent;

                        // 主表 auto.js（treeOnly 模式跳过）
                        if (!treeOnly)
                        {
                            DeleteLegacyPageAutoJs(viewPath, pt, entityLower);
                            var mainAutoPath = Path.Combine(viewPath, $"{entityLower}.{pt}.auto.js");
                            await File.WriteAllTextAsync(mainAutoPath, result.MainScriptContent);
                        }

                        // 主表 script.json（供设计器编辑 ScriptSection）
                        if (!string.IsNullOrEmpty(result.MainScriptJson))
                            await File.WriteAllTextAsync(Path.Combine(viewPath, $"{entityLower}.{pt}.script.json"), result.MainScriptJson);

                        // 字段级 ScriptSection（key=gen_id → ScriptSection JSON）
                        var fieldScriptsPath = Path.Combine(viewPath, $"{entityLower}.{pt}.fields.json");
                        if (result.FieldScripts.Count > 0)
                            await File.WriteAllTextAsync(fieldScriptsPath,
                                global::System.Text.Json.JsonSerializer.Serialize(result.FieldScripts));
                        else if (File.Exists(fieldScriptsPath))
                            File.Delete(fieldScriptsPath);

                        // 组件树 JSON（设计器直接加载，不再解析 .vue）
                        if (!string.IsNullOrEmpty(result.TreeJson))
                        {
                            var treeJsonPath = Path.Combine(viewPath, $"{entityLower}.{pt}.tree.json");
                            // 合并旧树的节点顺序（保留用户拖拽位置）
                            if (File.Exists(treeJsonPath))
                            {
                                try
                                {
                                    var oldTreeJson = await File.ReadAllTextAsync(treeJsonPath);
                                    result.TreeJson = incrementalChanges.Enabled
                                        ? MergeTreeIncremental(result.TreeJson, oldTreeJson, incrementalChanges)
                                        : MergeTreeOrder(result.TreeJson, oldTreeJson);
                                    finalTemplateHtml = SerializeTreeJsonToTemplate(result.TreeJson, result.VueContent);
                                }
                                catch { }
                            }
                            await File.WriteAllTextAsync(treeJsonPath, result.TreeJson);
                        }

                        // 子表 auto.js（treeOnly 模式跳过）
                        if (!treeOnly && result.ChildScripts.Count > 0)
                        {
                            foreach (var childScript in result.ChildScripts.Values)
                            {
                                var childName = childScript.EntityName;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                var childAutoPath = Path.Combine(viewPath, $"{childLower}.{pt}.auto.js");
                                await File.WriteAllTextAsync(childAutoPath, childScript.ScriptContent);
                            }
                        }
                        if (!treeOnly && result.ChildEntityNames.Count > 0)
                        {
                            foreach (var childName in result.ChildEntityNames)
                            {
                                if (result.ChildScripts.ContainsKey(childName)) continue;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                var childAutoPath = Path.Combine(viewPath, $"{childLower}.{pt}.auto.js");
                                if (File.Exists(childAutoPath)) File.Delete(childAutoPath);
                            }
                        }
                        // 子表 script.json + fields.json
                        if (result.ChildScripts.Count > 0)
                        {
                            foreach (var childScript in result.ChildScripts.Values)
                            {
                                if (string.IsNullOrEmpty(childScript.ScriptJson)) continue;
                                var childName = childScript.EntityName;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                await File.WriteAllTextAsync(Path.Combine(viewPath, $"{childLower}.{pt}.script.json"), childScript.ScriptJson);
                            }
                        }
                        if (result.ChildEntityNames.Count > 0)
                        {
                            foreach (var childName in result.ChildEntityNames)
                            {
                                if (result.ChildScripts.ContainsKey(childName)) continue;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                var childScriptPath = Path.Combine(viewPath, $"{childLower}.{pt}.script.json");
                                if (File.Exists(childScriptPath)) File.Delete(childScriptPath);
                            }
                        }
                        if (result.ChildFieldScriptsByEntity.Count > 0)
                        {
                            foreach (var kv in result.ChildFieldScriptsByEntity)
                            {
                                var childName = kv.Key;
                                if (string.IsNullOrWhiteSpace(childName) || kv.Value.Count == 0) continue;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                await File.WriteAllTextAsync(Path.Combine(viewPath, $"{childLower}.{pt}.fields.json"),
                                    global::System.Text.Json.JsonSerializer.Serialize(kv.Value));
                            }
                        }
                        if (result.ChildEntityNames.Count > 0)
                        {
                            foreach (var childName in result.ChildEntityNames)
                            {
                                if (result.ChildFieldScriptsByEntity.ContainsKey(childName)) continue;
                                var childLower = char.ToLowerInvariant(childName[0]) + childName[1..];
                                var childFieldsPath = Path.Combine(viewPath, $"{childLower}.{pt}.fields.json");
                                if (File.Exists(childFieldsPath)) File.Delete(childFieldsPath);
                            }
                        }

                        // .vue 文件：模板 + 导入语句（treeOnly 模式跳过）
                        if (!treeOnly)
                        {
                            var imports = result.VueImportLine;
                            if (!string.IsNullOrEmpty(result.ChildImportLine))
                                imports += "\n" + result.ChildImportLine;
                            var vueWithScript = BuildVueFileContent(finalTemplateHtml, imports, pt);
                            await WriteVueFileAsync(vuePath2, vueWithScript);
                        }
                    }
                    if (incrementalChanges.Enabled)
                    {
                        foreach (var childName in incrementalChanges.DeletedRelationNames)
                            await DeleteChildGeneratedFilesAsync(entity, childName);
                    }
                    entity.IsGenerated = true;
                    entity.LastGeneratedTime = DateTime.UtcNow;
                    await _db.Updateable(entity).UpdateColumns(e => new { e.IsGenerated, e.LastGeneratedTime }).ExecuteCommandAsync();
                    return;
                }

            }

        }

        // 更新实体状态
        entity.IsGenerated = true;
        entity.LastGeneratedTime = DateTime.UtcNow;
        await _db.Updateable(entity).ExecuteCommandAsync();

        // 子表不同步菜单
        if (!entity.IsChildTable)
        {
            var targetDb = GetTargetDbClient(project);
            await SyncMenuAndPermissionsAsync(entity, module, targetDb);
        }

    }

    /// <summary>
    /// 获取目标项目数据库的 SqlSugar 连接实例
    /// </summary>
    private SqlSugarClient GetTargetDbClient(Project project)
    {
        var connectionString = project.ConnectionString ?? string.Empty;
        if (project.DatabaseType == Core.Enums.DatabaseType.SQLite)
        {
            var match = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var dataSource = match.Groups[1].Value.Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(dataSource) && !Path.IsPathRooted(dataSource))
                {
                    var projectPath = ResolveProjectRootPath(project);
                    var absolutePath = Path.GetFullPath(Path.Combine(projectPath, $"{project.ProjectName}.Migrator", dataSource));
                    connectionString = Regex.Replace(connectionString, @"Data Source\s*=\s*([^;]+)", "Data Source=" + absolutePath, RegexOptions.IgnoreCase);
                }
            }
        }

        var dbType = project.DatabaseType switch
        {
            Core.Enums.DatabaseType.MySQL => DbType.MySql,
            Core.Enums.DatabaseType.SqlServer => DbType.SqlServer,
            Core.Enums.DatabaseType.PostgreSQL => DbType.PostgreSQL,
            Core.Enums.DatabaseType.SQLite => DbType.Sqlite,
            Core.Enums.DatabaseType.Oracle => DbType.Oracle,
            _ => DbType.SqlServer
        };

        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            ConfigureExternalServices = SqlSugarSetup.GetConfigureExternalServices(dbType)
        });
    }

    private static string ResolveProjectRootPath(Project project)
    {
        var projectPath = project.ProjectPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return projectPath;
        }

        return projectPath.EndsWith(project.ProjectName, StringComparison.OrdinalIgnoreCase)
            ? projectPath
            : Path.Combine(projectPath, project.ProjectName);
    }

    /// <summary>
    /// 同步菜单和权限到目标项目的 sys_menu 表
    /// </summary>
    private async Task SyncMenuAndPermissionsAsync(ModuleEntity entity, ProjectModule module, SqlSugarClient targetDb)
    {
        var moduleName = module.ModuleName;
        var permPrefix = $"{moduleName.ToLower()}:{entity.Name.ToLower()}";
        var entityNameLower = ToCamelCase(entity.Name);
        var entityTitleKey = ToCamelCase(entity.Name);
        var moduleNameLower = moduleName.ToLower();
        var moduleTitleKey = ToCamelCase(moduleName);
        var route = entity.FrontendRoute ?? $"/{moduleNameLower}/{entityNameLower}";

        // 1. 查找或创建目录菜单（模块级别）

        var dirMenu = await targetDb.Queryable<SysMenu>()
            .Where(m => m.Path == moduleNameLower && m.MenuType == "M" && m.ParentId == null)
            .FirstAsync();

        if (dirMenu == null)
        {
            dirMenu = new SysMenu
            {
                Id = GenerateSnowflakeId(),
                MenuName = module.ModuleDescription,
                TitleKey = moduleTitleKey,
                ParentId = null,
                OrderNum = 0,
                Path = moduleNameLower,
                MenuType = "M",
                Visible = true,
                Status = 0,
                Icon = "system",
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(dirMenu).ExecuteCommandAsync();
        }
        else
        {
            dirMenu.MenuName = module.ModuleDescription;
            dirMenu.TitleKey = moduleTitleKey;
            dirMenu.OrderNum = module.OrderNum;
            dirMenu.Icon = string.IsNullOrWhiteSpace(dirMenu.Icon) ? "system" : dirMenu.Icon;
            dirMenu.UpdateTime = DateTime.UtcNow;
            await targetDb.Updateable(dirMenu).ExecuteCommandAsync();
        }

        // 2. 查找或创建页面菜单（列表页）
        var pagePerms = $"{permPrefix}:list";
        var pagePath = entity.Name?.ToLower();
        var pageMenu = await targetDb.Queryable<SysMenu>()
            .Where(m => m.Path == pagePath && m.MenuType == "C" && m.ParentId == dirMenu.Id)
            .FirstAsync();

        if (pageMenu == null)
        {
            pageMenu = new SysMenu
            {
                Id = GenerateSnowflakeId(),
                MenuName = entity.Description,
                TitleKey = entityTitleKey,
                ParentId = dirMenu.Id,
                OrderNum = entity.OrderNum,
                Path = entityNameLower,
                Component = $"{moduleNameLower}/{entityNameLower}/index",
                MenuType = "C",
                Visible = true,
                Status = 0,
                Perms = pagePerms,
                Icon = string.IsNullOrWhiteSpace(entity.MenuIcon) ? "Document" : entity.MenuIcon,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(pageMenu).ExecuteCommandAsync();
        }
        else
        {
            pageMenu.MenuName = entity.Description;
            pageMenu.OrderNum = entity.OrderNum;
            pageMenu.TitleKey = entityTitleKey;
            pageMenu.Icon = entity.MenuIcon ?? "list";
            pageMenu.UpdateTime = DateTime.UtcNow;
            pageMenu.Path = entityNameLower;
            pageMenu.Component = $"{moduleNameLower}/{entityNameLower}/index";
            pageMenu.Perms = pagePerms;
            await targetDb.Updateable(pageMenu).ExecuteCommandAsync();
        }

        // 3. 创建按钮权限（非只读才有增删改）
        var buttonPerms = new List<(string name, string perm, int order)>
        {
            ("查询", $"{permPrefix}:list", 1),

        };

        if (!entity.IsReadOnly)
        {
            buttonPerms.Add(("新增", $"{permPrefix}:create", 3));
            buttonPerms.Add(("修改", $"{permPrefix}:update", 4));
            buttonPerms.Add(("删除", $"{permPrefix}:delete", 5));
        }

        foreach (var (name, perm, order) in buttonPerms)
        {
            var existing = await targetDb.Queryable<SysMenu>()
                .Where(m => m.Perms == perm && m.MenuType == "F" && m.ParentId == pageMenu.Id)
                .FirstAsync();

            if (existing == null)
            {
                var btnMenu = new SysMenu
                {
                    Id = GenerateSnowflakeId(),
                    MenuName = name,
                    ParentId = pageMenu.Id,
                    OrderNum = order,
                    MenuType = "F",
                    Visible = true,
                    Status = 0,
                    Perms = perm,
                    CreateTime = DateTime.UtcNow,
                    IsDeleted = false
                };
                await targetDb.Insertable(btnMenu).ExecuteCommandAsync();
            }
        }

        // 4. 同步多语言
        await SyncModuleLanguageAsync(module, targetDb);
        await SyncEntityLanguageAsync(entity, moduleName, targetDb);




        // 5. 创建隐藏的新增/编辑页面菜单（路由注册用，仅非只读）
        if (!entity.IsReadOnly)
        {
            // 新增页面菜单（隐藏，列表菜单的子菜单）
            var addPerms = $"{permPrefix}:create";
            var addMenu = await targetDb.Queryable<SysMenu>()
                .Where(m => m.Perms == addPerms && m.MenuType == "C" && m.ParentId == pageMenu.Id)
                .FirstAsync();

            if (addMenu == null)
            {
                addMenu = new SysMenu
                {
                    Id = GenerateSnowflakeId(),
                    MenuName = $"新增{entity.Description}",
                    TitleKey = null,
                    ParentId = pageMenu.Id,
                    OrderNum = 1,
                    Path = "add",
                    Component = $"{moduleNameLower}/{entityNameLower}/add",
                    MenuType = "C",
                    Visible = false,
                    Status = 0,
                    IsCache = true,
                    Perms = addPerms,
                    CreateTime = DateTime.UtcNow,
                    IsDeleted = false
                };
                await targetDb.Insertable(addMenu).ExecuteCommandAsync();
            }

            // 编辑页面菜单（隐藏，列表菜单的子菜单）
            var editPerms = $"{permPrefix}:update";
            var editMenu = await targetDb.Queryable<SysMenu>()
                .Where(m => m.Perms == editPerms && m.MenuType == "C" && m.ParentId == pageMenu.Id)
                .FirstAsync();

            if (editMenu == null)
            {
                editMenu = new SysMenu
                {
                    Id = GenerateSnowflakeId(),
                    MenuName = $"编辑{entity.Description}",
                    TitleKey = null,
                    ParentId = pageMenu.Id,
                    OrderNum = 2,
                    Path = "edit",
                    Component = $"{moduleNameLower}/{entityNameLower}/edit",
                    MenuType = "C",
                    Visible = false,
                    Status = 0,
                    IsCache = false,
                    Perms = editPerms,
                    CreateTime = DateTime.UtcNow,
                    IsDeleted = false
                };
                await targetDb.Insertable(editMenu).ExecuteCommandAsync();
            }
        }

        var detailPerms = $"{permPrefix}:detail";
        var detailMenu = await targetDb.Queryable<SysMenu>()
            .Where(m => m.Perms == detailPerms && m.MenuType == "C" && m.ParentId == pageMenu.Id)
            .FirstAsync();

        if (detailMenu == null)
        {
            detailMenu = new SysMenu
            {
                Id = GenerateSnowflakeId(),
                MenuName = $"{entity.Description}详情",
                TitleKey = null,
                ParentId = pageMenu.Id,
                OrderNum = 1,
                Path = "detail",
                Component = $"{moduleNameLower}/{entityNameLower}/detail",
                MenuType = "C",
                Visible = false,
                Status = 0,
                IsCache = false,
                Perms = detailPerms,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(detailMenu).ExecuteCommandAsync();
        }

    }

    /// <summary>
    /// 同步实体相关多语言到目标项目的 sys_lang_text 表
    /// </summary>
    private async Task SyncEntityLanguageAsync(ModuleEntity entity, string moduleName, SqlSugarClient targetDb)
    {
        var entityNameLower = ToCamelCase(entity.Name);
        var entries = new List<(string langKey, string zhCN, string enUS)>
        {
            // 实体标题（菜单显示名）
            (entityNameLower, entity.Description ?? entity.Name, entity.Name),
            // 按钮权限
            ($"{entityNameLower}_list", "查询", "Query"),
            ($"{entityNameLower}_detail", "详情", "Detail"),
        };

        if (!entity.IsReadOnly)
        {
            entries.Add(($"{entityNameLower}_create", "新增", "Create"));
            entries.Add(($"{entityNameLower}_update", "修改", "Update"));
            entries.Add(($"{entityNameLower}_delete", "删除", "Delete"));
        }

        foreach (var (langKey, zhCN, enUS) in entries)
        {
            await UpsertLangTextAsync(targetDb, langKey, "zh-CN", zhCN);
            await UpsertLangTextAsync(targetDb, langKey, "en-US", enUS);
        }
    }

    private async Task SyncModuleLanguageAsync(ProjectModule module, SqlSugarClient targetDb)
    {
        var moduleTitleKey = ToCamelCase(module.ModuleName);
        await UpsertLangTextAsync(targetDb, moduleTitleKey, "zh-CN", module.ModuleDescription ?? module.ModuleName);
        await UpsertLangTextAsync(targetDb, moduleTitleKey, "en-US", module.ModuleName);
    }

    private async Task SyncEntityAndFieldLanguageAsync(ModuleEntity entity, SqlSugarClient targetDb)
    {
        await SyncEntityLanguageAsync(entity, string.Empty, targetDb);

        var fields = await _db.Queryable<EntityField>()
            .Where(f => f.ModuleEntityId == entity.Id)
            .OrderBy(f => f.OrderNum)
            .ToListAsync();

        foreach (var field in fields)
        {
            var key = ToCamelCase(field.Name);
            await UpsertLangTextAsync(targetDb, key, "zh-CN", field.Description ?? field.Name);
            await UpsertLangTextAsync(targetDb, key, "en-US", field.Name);
        }
    }

    private async Task UpsertLangTextAsync(SqlSugarClient targetDb, string langKey, string langCode, string langValue)
    {
        var existing = await targetDb.Queryable<SysLangText>()
            .Where(t => t.LangKey == langKey && t.LangCode == langCode)
            .FirstAsync();

        if (existing != null)
        {
            existing.LangValue = langValue;
            existing.UpdateTime = DateTime.UtcNow;
            await targetDb.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            var text = new SysLangText
            {
                Id = GenerateSnowflakeId(),
                LangKey = langKey,
                LangCode = langCode,
                LangValue = langValue,
                CreateTime = DateTime.UtcNow,
                IsDeleted = false
            };
            await targetDb.Insertable(text).ExecuteCommandAsync();
        }
    }

    private long GenerateSnowflakeId() => DateTime.UtcNow.Ticks;

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// 标记子表实体为被引用状态
    /// </summary>
    private async Task MarkChildEntityAsReferenced(long childEntityId)
    {
        await _db.Updateable<ModuleEntity>()
            .SetColumns(e => e.IsChildTable == true)
            .Where(e => e.Id == childEntityId)
            .ExecuteCommandAsync();
    }

    private async Task WriteVueFileAsync(string filePath, string newContent)
    {
        await File.WriteAllTextAsync(filePath, newContent);
    }

    private static string BuildVueFileContent(string templateHtml, string scriptSetupContent, string pageType)
    {
        var normalizedTemplate = templateHtml?.Trim() ?? string.Empty;
        if (!StartsWithTemplateTag(normalizedTemplate))
        {
            normalizedTemplate = "<template>" + Environment.NewLine
                + normalizedTemplate + Environment.NewLine
                + "</template>";
        }

        return normalizedTemplate
            + Environment.NewLine
            + "<script setup>"
            + Environment.NewLine
            + (scriptSetupContent ?? string.Empty).Trim()
            + Environment.NewLine
            + "</script>"
            + Environment.NewLine
            + BuildStyleContent(pageType);
    }

    private static string BuildStyleContent(string pageType)
    {
        if (string.Equals(pageType, "index", StringComparison.OrdinalIgnoreCase))
        {
            return "<style scoped lang=\"scss\">"
                + Environment.NewLine
                + "@import '@/styles/list-page.scss';"
                + Environment.NewLine
                + "</style>";
        }

        return "<style scoped>"
            + Environment.NewLine
            + "</style>";
    }

    private static bool StartsWithTemplateTag(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        return global::System.Text.RegularExpressions.Regex.IsMatch(
            content,
            @"^\s*<template(?:\s|>|$)",
            global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 获取页面模板内容（可视化设计器加载用）
    /// </summary>
    public async Task<PageContentDto> GetPageContentAsync(long id, string pageType)
    {
        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");

        var filePath = await ResolveVueFilePath(entity, pageType);
        var fullContent = string.Empty;
        if (File.Exists(filePath))
            fullContent = await File.ReadAllTextAsync(filePath);

        var parser = new CodeMaster.Infrastructure.VueParser.VueTemplateParser();
        var templateContent = ExtractTemplateContent(fullContent);

        // 优先从 .tree.json 加载（生成时预解析的组件树）
        string? treeJson = null;
        var treeJsonPath = await ResolveTreeJsonPath(entity, pageType);
        if (File.Exists(treeJsonPath))
        {
            treeJson = await File.ReadAllTextAsync(treeJsonPath);
        }
        else if (!string.IsNullOrEmpty(fullContent))
        {
            // 回退：解析 .vue 文件
            try
            {
                var tree = parser.Parse(fullContent);
                treeJson = global::System.Text.Json.JsonSerializer.Serialize(tree, new global::System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase
                });
            }
            catch { /* 解析失败时 treeJson 为 null */ }
        }

        // 提取 <style> 部分
        var styleContent = ExtractStyleContent(fullContent);

        return new PageContentDto
        {
            PageType = pageType,
            FileName = Path.GetFileName(filePath),
            TemplateHtml = templateContent,
            FullContent = fullContent,
            TreeJson = treeJson,
            StyleContent = styleContent
        };
    }

    /// <summary>
    /// 保存页面模板内容（可视化设计器回写）
    /// 只替换 <template>...</template> 部分，script 和 style 保持不变
    /// </summary>
    public async Task<bool> SavePageContentAsync(long id, string pageType, SavePageContentDto input)
    {
        if (input == null)
            throw new ArgumentException("SavePageContent input cannot be null.", nameof(input));

        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");

        var filePath = await ResolveVueFilePath(entity, pageType);
        if (!File.Exists(filePath))
            throw new Exception($"页面文件不存在: {filePath}");

        var fullContent = await File.ReadAllTextAsync(filePath);

        // TreeJson 优先（可视化编辑器模式）
        if (!string.IsNullOrWhiteSpace(input.TreeJson))
        {
            var tree = global::System.Text.Json.JsonSerializer.Deserialize<List<CodeMaster.Infrastructure.VueParser.Model.Component>>(
                input.TreeJson,
                new global::System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase });

            if (tree != null)
            {
                var serializer = new CodeMaster.Infrastructure.VueParser.VueTemplateSerializer();
                var templateHtml = serializer.Serialize(tree);
                var newContent = ReplaceTemplateContent(fullContent, templateHtml);
                await File.WriteAllTextAsync(filePath, newContent);

                var treeJsonPath = await ResolveTreeJsonPath(entity, pageType);
                await File.WriteAllTextAsync(treeJsonPath, input.TreeJson);

                // 清理被删除控件的字段 ScriptSection
                await CleanupRemovedFieldScripts(entity, pageType, tree);

                return true;
            }
        }

        // 回退：直接用 TemplateHtml
        if (!string.IsNullOrWhiteSpace(input.TemplateHtml))
        {
            var newContent = ReplaceTemplateContent(fullContent, input.TemplateHtml);
            await File.WriteAllTextAsync(filePath, newContent);
        }

        return true;
    }

    public async Task<string?> GetPageScriptAsync(long id, string pageType)
    {
        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");
        var scriptPath = await ResolveScriptFilePath(entity, pageType);
        if (!File.Exists(scriptPath)) return null;
        return await File.ReadAllTextAsync(scriptPath);
    }

    public async Task<bool> SavePageScriptAsync(long id, string pageType, string scriptJson)
    {
        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");
        var scriptPath = await ResolveScriptFilePath(entity, pageType);
        await File.WriteAllTextAsync(scriptPath, scriptJson);
        return true;
    }

    public async Task<Dictionary<string, string>?> GetFieldScriptsAsync(long id, string pageType)
    {
        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");
        var vuePath = await ResolveVueFilePath(entity, pageType);
        var dir = Path.GetDirectoryName(vuePath)!;

        var allFieldScripts = new Dictionary<string, string>();

        // 加载所有 *.fields.json（主表 + 子表字段级）
        if (Directory.Exists(dir))
        {
            foreach (var fp in Directory.GetFiles(dir, $"*.{pageType}.fields.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(fp);
                    var dict = global::System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                            allFieldScripts[kv.Key] = kv.Value;
                    }
                }
                catch { }
            }

            // 也加载子表页面级 script.json（按 child_ 前缀放入）
            foreach (var fp in Directory.GetFiles(dir, $"*.{pageType}.script.json"))
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(fp); // e.g. "orderItem.add.script" → "orderItem.add"
                    var prefix = Path.GetFileNameWithoutExtension(fileName); // "orderItem.add"
                    var json = await File.ReadAllTextAsync(fp);
                    // 跳过主表自己的 script.json
                    var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
                    if (!fileName.StartsWith(entityLower + "."))
                    {
                        allFieldScripts[$"child_{prefix}"] = json;
                    }
                }
                catch { }
            }
        }

        return allFieldScripts.Count > 0 ? allFieldScripts : null;
    }

    public async Task<bool> SaveFieldScriptsAsync(long id, string pageType, Dictionary<string, string> scripts)
    {
        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == id).FirstAsync();
        if (entity == null) throw new Exception($"实体不存在: {id}");
        var vuePath = await ResolveVueFilePath(entity, pageType);
        var dir = Path.GetDirectoryName(vuePath)!;
        var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
        var fieldsPath = Path.Combine(dir, $"{entityLower}.{pageType}.fields.json");
        var json = global::System.Text.Json.JsonSerializer.Serialize(scripts);
        await File.WriteAllTextAsync(fieldsPath, json);
        return true;
    }

    /// <summary>清理被删除控件的字段级 ScriptSection</summary>
    private async Task CleanupRemovedFieldScripts(ModuleEntity entity, string pageType,
        List<CodeMaster.Infrastructure.VueParser.Model.Component> newTree)
    {
        var vuePath = await ResolveVueFilePath(entity, pageType);
        var dir = Path.GetDirectoryName(vuePath)!;

        var newIds = new HashSet<string>();
        var activeTables = new HashSet<string>();  // 新树中存在的子表名
        CollectNodeInfo(newTree, newIds, activeTables);

        if (Directory.Exists(dir))
        {
            foreach (var fp in Directory.GetFiles(dir, $"*.{pageType}.fields.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(fp);
                    var dict = global::System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict == null) continue;

                    var toRemove = new List<string>();
                    foreach (var (k, v) in dict)
                    {
                        // 1. genId 不在新树中 → 删除
                        if (!newIds.Contains(k)) { toRemove.Add(k); continue; }

                        // 2. 检查 tableId：如果该脚本属于某个子表，但子表卡片已被删除 → 删除
                        try
                        {
                            var entry = global::System.Text.Json.JsonDocument.Parse(v).RootElement;
                            if (entry.TryGetProperty("tableId", out var tid) && !string.IsNullOrEmpty(tid.GetString()))
                            {
                                if (!activeTables.Contains(tid.GetString()!))
                                    toRemove.Add(k);
                            }
                        }
                        catch { }
                    }

                    if (toRemove.Count == 0) continue;
                    foreach (var k in toRemove) dict.Remove(k);
                    await File.WriteAllTextAsync(fp, global::System.Text.Json.JsonSerializer.Serialize(dict));
                }
                catch { }
            }
        }
    }

    private static void CollectNodeInfo(List<CodeMaster.Infrastructure.VueParser.Model.Component> nodes,
        HashSet<string> ids, HashSet<string> tables)
    {
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.GenId))
            {
                ids.Add(n.GenId);
                // gen_child_X → 记录子表名 X
                if (n.GenId.StartsWith("gen_child_"))
                    tables.Add(n.GenId["gen_child_".Length..]);
            }
            if (!string.IsNullOrEmpty(n.EntityTable))
                tables.Add(n.EntityTable);

            if (n.Children != null) CollectNodeInfo(n.Children, ids, tables);
            if (n.UseSlots != null)
            {
                foreach (var s in n.UseSlots)
                    if (s.Components != null) CollectNodeInfo(s.Components, ids, tables);
            }
        }
    }

    private async Task<string> ResolveScriptFilePath(ModuleEntity entity, string pageType)
    {
        var vuePath = await ResolveVueFilePath(entity, pageType);
        var dir = Path.GetDirectoryName(vuePath)!;
        var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
        return Path.Combine(dir, $"{entityLower}.{pageType}.script.json");
    }

    /// <summary>合并新旧组件树的节点顺序（保留用户拖拽位置）</summary>
    private static string MergeTreeOrder(string newTreeJson, string oldTreeJson)
    {
        if (string.IsNullOrWhiteSpace(oldTreeJson)) return newTreeJson;
        // 用 JsonNode 操作顶级 children 顺序
        var oldNode = global::System.Text.Json.Nodes.JsonNode.Parse(oldTreeJson);
        var newNode = global::System.Text.Json.Nodes.JsonNode.Parse(newTreeJson);
        if (oldNode is not global::System.Text.Json.Nodes.JsonArray oldArr) return newTreeJson;
        if (newNode is not global::System.Text.Json.Nodes.JsonArray newArr) return newTreeJson;

        // 建立 genId → position 映射
        var orderMap = new Dictionary<string, int>();
        var scriptMap = new Dictionary<string, string>();
        CollectTreeScriptSections(oldArr, scriptMap);
        for (int i = 0; i < oldArr.Count; i++)
        {
            var genId = oldArr[i]?["genId"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(genId) && !orderMap.ContainsKey(genId))
                orderMap[genId] = i;
        }

        if (orderMap.Count == 0 && scriptMap.Count == 0) return newTreeJson;

        if (scriptMap.Count > 0)
            ApplyTreeScriptSections(newArr, scriptMap);

        // 对新树每个节点递归应用顺序
        if (orderMap.Count > 0)
            ReorderByOldMap(newArr, orderMap);

        return newNode.ToJsonString(new global::System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<IncrementalChangeSet> BuildIncrementalChangeSetAsync(ModuleEntity entity)
    {
        if (!entity.IsGenerated || entity.LastGeneratedTime == null)
            return IncrementalChangeSet.Disabled;

        var since = entity.LastGeneratedTime.Value;
        var changes = new IncrementalChangeSet
        {
            Enabled = true,
            Since = since
        };

        var fields = await _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(f => f.ModuleEntityId == entity.Id)
            .ToListAsync();
        CollectChangedFields(fields, since, changes);

        if (!_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
            return changes;

        var relations = await _db.Queryable<OneToManyRelation>()
            .ClearFilter()
            .Where(r => r.ModuleEntityId == entity.Id)
            .ToListAsync();

        foreach (var relation in relations)
        {
            var relationName = await ResolveRelationChildNameAsync(relation);
            var relationChanged = HasChangedSince(relation.CreateTime, relation.UpdateTime, since);
            var relationDeleted = relation.IsDeleted && HasChangedSince(relation.CreateTime, relation.DeleteTime ?? relation.UpdateTime, since);

            if (!string.IsNullOrWhiteSpace(relationName))
            {
                if (relationDeleted)
                    changes.DeletedRelationNames.Add(relationName);
                else if (relationChanged)
                    changes.ChangedRelationNames.Add(relationName);
            }

            if (relation.IsDeleted)
                continue;

            var childFields = await _db.Queryable<EntityField>()
                .ClearFilter()
                .Where(f => f.ModuleEntityId == relation.ChildEntityId)
                .ToListAsync();
            var changedFieldCount = changes.ChangedFieldIds.Count;
            var deletedFieldCount = changes.DeletedFieldIds.Count;
            CollectChangedFields(childFields, since, changes);

            var childFieldsChanged = changes.ChangedFieldIds.Count != changedFieldCount ||
                                     changes.DeletedFieldIds.Count != deletedFieldCount;
            if (childFieldsChanged && !string.IsNullOrWhiteSpace(relationName))
                changes.ChangedRelationNames.Add(relationName);
        }

        return changes;
    }

    private async Task<string> ResolveRelationChildNameAsync(OneToManyRelation relation)
    {
        if (!string.IsNullOrWhiteSpace(relation.ChildEntityName))
            return relation.ChildEntityName;

        var child = await _db.Queryable<ModuleEntity>()
            .ClearFilter()
            .Where(e => e.Id == relation.ChildEntityId)
            .FirstAsync();

        return child?.Name ?? string.Empty;
    }

    private static void CollectChangedFields(IEnumerable<EntityField> fields, DateTime since, IncrementalChangeSet changes)
    {
        foreach (var field in fields)
        {
            if (field.IsDeleted)
            {
                if (HasChangedSince(field.CreateTime, field.DeleteTime ?? field.UpdateTime, since))
                    changes.DeletedFieldIds.Add(field.Id);
                continue;
            }

            if (HasChangedSince(field.CreateTime, field.UpdateTime, since))
                changes.ChangedFieldIds.Add(field.Id);
        }
    }

    private static bool HasChangedSince(DateTime createTime, DateTime? updateTime, DateTime since)
    {
        return createTime > since || (updateTime.HasValue && updateTime.Value > since);
    }

    private static string MergeTreeIncremental(string newTreeJson, string oldTreeJson, IncrementalChangeSet changes)
    {
        if (string.IsNullOrWhiteSpace(oldTreeJson)) return newTreeJson;

        var newTree = DeserializeComponentTree(newTreeJson);
        var oldTree = DeserializeComponentTree(oldTreeJson);
        if (newTree == null || oldTree == null) return newTreeJson;

        var merged = MergeComponentList(newTree, oldTree, changes);
        return global::System.Text.Json.JsonSerializer.Serialize(merged, GetTreeJsonOptions());
    }

    private static string SerializeTreeJsonToTemplate(string treeJson, string fallbackTemplateHtml)
    {
        try
        {
            var tree = DeserializeComponentTree(treeJson);
            if (tree == null) return fallbackTemplateHtml;

            var serializer = new VueTemplateSerializer();
            return serializer.Serialize(tree);
        }
        catch
        {
            return fallbackTemplateHtml;
        }
    }

    private static List<Component>? DeserializeComponentTree(string treeJson)
    {
        if (string.IsNullOrWhiteSpace(treeJson)) return null;

        return global::System.Text.Json.JsonSerializer.Deserialize<List<Component>>(treeJson, GetTreeJsonOptions());
    }

    private static global::System.Text.Json.JsonSerializerOptions GetTreeJsonOptions()
    {
        return new global::System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private static List<Component>? MergeComponentList(
        List<Component>? newNodes,
        List<Component>? oldNodes,
        IncrementalChangeSet changes)
    {
        if (newNodes == null || newNodes.Count == 0)
            return PreserveCustomOldNodes(oldNodes);

        if (oldNodes == null || oldNodes.Count == 0)
            return CloneComponentList(newNodes);

        var merged = new List<Component>();
        var usedOldIndexes = new HashSet<int>();

        for (var i = 0; i < newNodes.Count; i++)
        {
            var newNode = newNodes[i];
            var genId = GetOwnGenId(newNode);
            var oldIndex = !string.IsNullOrWhiteSpace(genId)
                ? FindOldIndexByGenId(oldNodes, usedOldIndexes, genId!)
                : FindOldStructuralIndex(oldNodes, usedOldIndexes, newNode, i);

            if (oldIndex < 0)
            {
                merged.Add(CloneComponent(newNode));
                continue;
            }

            usedOldIndexes.Add(oldIndex);
            var oldNode = oldNodes[oldIndex];

            if (!string.IsNullOrWhiteSpace(genId) &&
                IsKnownGeneratedGenId(genId!) &&
                !IsGeneratedContainerGenId(genId!) &&
                !changes.IsChangedGeneratedId(genId!))
            {
                merged.Add(CloneComponent(oldNode));
                continue;
            }

            var preferOldShell = string.IsNullOrWhiteSpace(genId) && !oldNode.IsDeleted;
            var mergedNode = preferOldShell ? CloneComponent(oldNode) : CloneComponent(newNode);
            if (!preferOldShell)
                PreserveScriptSectionIfNeeded(mergedNode, oldNode);

            mergedNode.Children = MergeComponentList(newNode.Children, oldNode.Children, changes);
            mergedNode.UseSlots = MergeComponentSlots(newNode.UseSlots, oldNode.UseSlots, changes);
            merged.Add(mergedNode);
        }

        for (var i = 0; i < oldNodes.Count; i++)
        {
            if (usedOldIndexes.Contains(i)) continue;
            var oldNode = oldNodes[i];
            if (ShouldPreserveCustomOldNode(oldNode))
                merged.Add(CloneComponent(oldNode));
        }

        return merged;
    }

    private static List<ComponentSlot>? MergeComponentSlots(
        List<ComponentSlot>? newSlots,
        List<ComponentSlot>? oldSlots,
        IncrementalChangeSet changes)
    {
        if (newSlots == null || newSlots.Count == 0)
            return PreserveCustomOldSlots(oldSlots);

        if (oldSlots == null || oldSlots.Count == 0)
            return CloneSlotList(newSlots);

        var merged = new List<ComponentSlot>();
        var usedOldIndexes = new HashSet<int>();

        foreach (var newSlot in newSlots)
        {
            var oldIndex = FindOldSlotIndex(oldSlots, usedOldIndexes, newSlot);
            if (oldIndex < 0)
            {
                merged.Add(CloneSlot(newSlot));
                continue;
            }

            usedOldIndexes.Add(oldIndex);
            var oldSlot = oldSlots[oldIndex];
            var mergedSlot = CloneSlot(newSlot);
            mergedSlot.Components = MergeComponentList(newSlot.Components, oldSlot.Components, changes);
            merged.Add(mergedSlot);
        }

        for (var i = 0; i < oldSlots.Count; i++)
        {
            if (usedOldIndexes.Contains(i)) continue;
            var oldSlot = oldSlots[i];
            if (!ContainsGeneratedIdentity(oldSlot))
                merged.Add(CloneSlot(oldSlot));
        }

        return merged.Count > 0 ? merged : null;
    }

    private static int FindOldIndexByGenId(List<Component> oldNodes, HashSet<int> usedOldIndexes, string genId)
    {
        for (var i = 0; i < oldNodes.Count; i++)
        {
            if (usedOldIndexes.Contains(i)) continue;
            if (string.Equals(GetOwnGenId(oldNodes[i]), genId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static int FindOldStructuralIndex(
        List<Component> oldNodes,
        HashSet<int> usedOldIndexes,
        Component newNode,
        int preferredIndex)
    {
        if (preferredIndex >= 0 &&
            preferredIndex < oldNodes.Count &&
            !usedOldIndexes.Contains(preferredIndex) &&
            IsStructuralMatch(newNode, oldNodes[preferredIndex]))
        {
            return preferredIndex;
        }

        for (var i = 0; i < oldNodes.Count; i++)
        {
            if (usedOldIndexes.Contains(i)) continue;
            if (IsStructuralMatch(newNode, oldNodes[i]))
                return i;
        }

        return -1;
    }

    private static bool IsStructuralMatch(Component newNode, Component oldNode)
    {
        return string.IsNullOrWhiteSpace(GetOwnGenId(oldNode)) &&
               string.Equals(newNode.Tag, oldNode.Tag, StringComparison.OrdinalIgnoreCase);
    }

    private static int FindOldSlotIndex(List<ComponentSlot> oldSlots, HashSet<int> usedOldIndexes, ComponentSlot newSlot)
    {
        for (var i = 0; i < oldSlots.Count; i++)
        {
            if (usedOldIndexes.Contains(i)) continue;
            var oldSlot = oldSlots[i];
            if (string.Equals(oldSlot.Name, newSlot.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oldSlot.Parameter ?? string.Empty, newSlot.Parameter ?? string.Empty, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetOwnGenId(Component component)
    {
        return string.IsNullOrWhiteSpace(component.GenId) ? null : component.GenId;
    }

    private static bool IsKnownGeneratedGenId(string genId)
    {
        return genId.StartsWith("gen_field_", StringComparison.OrdinalIgnoreCase) ||
               genId.StartsWith("gen_col_", StringComparison.OrdinalIgnoreCase) ||
               genId.StartsWith("gen_child_", StringComparison.OrdinalIgnoreCase) ||
               genId.StartsWith("gen_action_", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_toolbar", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_operations", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_pagination", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_search_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_list_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_form_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_detail_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_form_actions", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedContainerGenId(string genId)
    {
        return string.Equals(genId, "gen_search_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_list_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_form_area", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(genId, "gen_detail_area", StringComparison.OrdinalIgnoreCase) ||
               genId.StartsWith("gen_child_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsGeneratedIdentity(Component component)
    {
        if (!string.IsNullOrWhiteSpace(component.GenId) && IsKnownGeneratedGenId(component.GenId))
            return true;
        if (!string.IsNullOrWhiteSpace(component.EntityField) || !string.IsNullOrWhiteSpace(component.EntityTable))
            return true;

        if (component.Children != null && component.Children.Any(ContainsGeneratedIdentity))
            return true;

        return component.UseSlots != null && component.UseSlots.Any(ContainsGeneratedIdentity);
    }

    private static bool ContainsGeneratedIdentity(ComponentSlot slot)
    {
        return slot.Components != null && slot.Components.Any(ContainsGeneratedIdentity);
    }

    private static List<Component>? PreserveCustomOldNodes(List<Component>? oldNodes)
    {
        if (oldNodes == null || oldNodes.Count == 0) return null;

        var custom = oldNodes.Where(ShouldPreserveCustomOldNode).Select(CloneComponent).ToList();
        return custom.Count > 0 ? custom : null;
    }

    private static List<ComponentSlot>? PreserveCustomOldSlots(List<ComponentSlot>? oldSlots)
    {
        if (oldSlots == null || oldSlots.Count == 0) return null;

        var custom = oldSlots.Where(slot => !ContainsGeneratedIdentity(slot)).Select(CloneSlot).ToList();
        return custom.Count > 0 ? custom : null;
    }

    private static bool ShouldPreserveCustomOldNode(Component oldNode)
    {
        return !oldNode.IsDeleted &&
               !string.Equals(oldNode.Tag, "text", StringComparison.OrdinalIgnoreCase) &&
               !ContainsGeneratedIdentity(oldNode);
    }

    private static void PreserveScriptSectionIfNeeded(Component target, Component source)
    {
        if (!string.IsNullOrWhiteSpace(target.ScriptSection)) return;
        if (string.IsNullOrWhiteSpace(source.ScriptSection)) return;
        if (!IsMeaningfulTreeScriptSection(source.ScriptSection)) return;

        target.ScriptSection = source.ScriptSection;
    }

    private static List<Component> CloneComponentList(IEnumerable<Component> components)
    {
        return components.Select(CloneComponent).ToList();
    }

    private static List<ComponentSlot> CloneSlotList(IEnumerable<ComponentSlot> slots)
    {
        return slots.Select(CloneSlot).ToList();
    }

    private static Component CloneComponent(Component component)
    {
        var json = global::System.Text.Json.JsonSerializer.Serialize(component, GetTreeJsonOptions());
        return global::System.Text.Json.JsonSerializer.Deserialize<Component>(json, GetTreeJsonOptions()) ?? new Component();
    }

    private static ComponentSlot CloneSlot(ComponentSlot slot)
    {
        var json = global::System.Text.Json.JsonSerializer.Serialize(slot, GetTreeJsonOptions());
        return global::System.Text.Json.JsonSerializer.Deserialize<ComponentSlot>(json, GetTreeJsonOptions()) ?? new ComponentSlot();
    }

    private sealed class IncrementalChangeSet
    {
        public static IncrementalChangeSet Disabled => new() { Enabled = false };

        public bool Enabled { get; init; }
        public DateTime? Since { get; init; }
        public HashSet<long> ChangedFieldIds { get; } = new();
        public HashSet<long> DeletedFieldIds { get; } = new();
        public HashSet<string> ChangedRelationNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> DeletedRelationNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IsChangedGeneratedId(string genId)
        {
            if (TryParseGeneratedFieldId(genId, out var fieldId))
                return ChangedFieldIds.Contains(fieldId) || DeletedFieldIds.Contains(fieldId);

            const string childPrefix = "gen_child_";
            if (genId.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var childName = genId[childPrefix.Length..];
                return ChangedRelationNames.Contains(childName) || DeletedRelationNames.Contains(childName);
            }

            return false;
        }

        private static bool TryParseGeneratedFieldId(string genId, out long fieldId)
        {
            fieldId = 0;
            var prefix = genId.StartsWith("gen_field_", StringComparison.OrdinalIgnoreCase)
                ? "gen_field_"
                : genId.StartsWith("gen_col_", StringComparison.OrdinalIgnoreCase)
                    ? "gen_col_"
                    : null;

            if (prefix == null) return false;

            var start = prefix.Length;
            var end = start;
            while (end < genId.Length && char.IsDigit(genId[end]))
                end++;

            return end > start && long.TryParse(genId[start..end], out fieldId);
        }
    }

    private static void CollectTreeScriptSections(
        global::System.Text.Json.Nodes.JsonArray nodes,
        Dictionary<string, string> scriptMap)
    {
        foreach (var node in nodes)
        {
            var genId = node?["genId"]?.GetValue<string>();
            var scriptSection = node?["scriptSection"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(genId) &&
                !string.IsNullOrWhiteSpace(scriptSection) &&
                IsMeaningfulTreeScriptSection(scriptSection) &&
                !scriptMap.ContainsKey(genId))
            {
                scriptMap[genId] = scriptSection;
            }

            var children = node?["children"] as global::System.Text.Json.Nodes.JsonArray;
            if (children != null) CollectTreeScriptSections(children, scriptMap);

            var slots = node?["useSlots"] as global::System.Text.Json.Nodes.JsonArray;
            if (slots == null) continue;
            foreach (var slot in slots)
            {
                var components = slot?["components"] as global::System.Text.Json.Nodes.JsonArray;
                if (components != null) CollectTreeScriptSections(components, scriptMap);
            }
        }
    }

    private static bool IsMeaningfulTreeScriptSection(string scriptSection)
    {
        try
        {
            var scriptJson = ExtractTreeScriptJson(scriptSection);
            if (string.IsNullOrWhiteSpace(scriptJson)) return false;

            using var doc = global::System.Text.Json.JsonDocument.Parse(scriptJson);
            if (doc.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
                return true;

            foreach (var propertyName in new[]
            {
                "imports", "uses", "consts", "lets", "refs", "reactives", "functions",
                "hooks", "computed", "watches", "dictRefs"
            })
            {
                if (doc.RootElement.TryGetProperty(propertyName, out var property) &&
                    property.ValueKind == global::System.Text.Json.JsonValueKind.Array &&
                    property.GetArrayLength() > 0)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return true;
        }
    }

    private static string ExtractTreeScriptJson(string raw)
    {
        var scriptJson = raw;
        for (var i = 0; i < 3; i++)
        {
            if (string.IsNullOrWhiteSpace(scriptJson)) return string.Empty;

            using var doc = global::System.Text.Json.JsonDocument.Parse(scriptJson);
            if (doc.RootElement.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                scriptJson = doc.RootElement.GetString() ?? string.Empty;
                continue;
            }

            if (doc.RootElement.ValueKind == global::System.Text.Json.JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("script", out var inner))
            {
                scriptJson = inner.ValueKind == global::System.Text.Json.JsonValueKind.String
                    ? inner.GetString() ?? string.Empty
                    : inner.GetRawText();
                continue;
            }

            return doc.RootElement.GetRawText();
        }

        return scriptJson;
    }

    private static void ApplyTreeScriptSections(
        global::System.Text.Json.Nodes.JsonArray nodes,
        Dictionary<string, string> scriptMap)
    {
        foreach (var node in nodes)
        {
            var genId = node?["genId"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(genId) && scriptMap.TryGetValue(genId, out var scriptSection))
                node!["scriptSection"] = scriptSection;

            var children = node?["children"] as global::System.Text.Json.Nodes.JsonArray;
            if (children != null) ApplyTreeScriptSections(children, scriptMap);

            var slots = node?["useSlots"] as global::System.Text.Json.Nodes.JsonArray;
            if (slots == null) continue;
            foreach (var slot in slots)
            {
                var components = slot?["components"] as global::System.Text.Json.Nodes.JsonArray;
                if (components != null) ApplyTreeScriptSections(components, scriptMap);
            }
        }
    }

    private static void ReorderByOldMap(global::System.Text.Json.Nodes.JsonArray parent, Dictionary<string, int> orderMap)
    {
        // 收集当前层级有 genId 的节点
        var indexed = new List<(int oldPos, global::System.Text.Json.Nodes.JsonNode? node)>();
        var unindexed = new List<global::System.Text.Json.Nodes.JsonNode?>();
        foreach (var child in parent)
        {
            var genId = child?["genId"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(genId) && orderMap.TryGetValue(genId, out var pos))
                indexed.Add((pos, child));
            else
                unindexed.Add(child);
        }

        if (indexed.Count == 0) return;

        // 按旧顺序排列
        indexed.Sort((a, b) => a.oldPos.CompareTo(b.oldPos));
        parent.Clear();
        // 合并：已索引节点按旧顺序，新节点插在末尾
        foreach (var item in indexed) parent.Add(item.node);
        foreach (var item in unindexed) parent.Add(item);

        // 递归处理子节点
        foreach (var child in parent)
        {
            var children = child?["children"] as global::System.Text.Json.Nodes.JsonArray;
            if (children != null) ReorderByOldMap(children, orderMap);
            // 也处理插槽内的组件
            var slots = child?["useSlots"] as global::System.Text.Json.Nodes.JsonArray;
            if (slots != null)
            {
                foreach (var s in slots)
                {
                    var comps = s?["components"] as global::System.Text.Json.Nodes.JsonArray;
                    if (comps != null) ReorderByOldMap(comps, orderMap);
                }
            }
        }
    }

    private async Task<string> ResolveTreeJsonPath(ModuleEntity entity, string pageType)
    {
        var vuePath = await ResolveVueFilePath(entity, pageType);
        var dir = Path.GetDirectoryName(vuePath)!;
        var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
        return Path.Combine(dir, $"{entityLower}.{pageType}.tree.json");
    }

    /// <summary>
    /// 解析 .vue 文件路径
    /// </summary>
    private async Task<string> ResolveVueFilePath(ModuleEntity entity, string pageType)
    {
        var module = await _db.Queryable<ProjectModule>().Where(m => m.Id == entity.ModuleId).FirstAsync();
        if (module == null) throw new Exception($"模块不存在: {entity.ModuleId}");

        var project = await _db.Queryable<Project>().Where(p => p.Id == module.ProjectId).FirstAsync();
        if (project == null) throw new Exception($"项目不存在: {module.ProjectId}");

        var projectPath = project.ProjectPath;
        if (string.IsNullOrWhiteSpace(projectPath))
            projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        var entityNameLower = entity.Name.ToLower();
        var moduleNameLower = module.ModuleName.ToLower();
        var viewPath = Path.Combine(projectPath, $"{project.ProjectName}.Vue", "src", "views", moduleNameLower, entityNameLower);

        return pageType switch
        {
            "index" => Path.Combine(viewPath, "index.vue"),
            "add" => Path.Combine(viewPath, "add.vue"),
            "edit" => Path.Combine(viewPath, "edit.vue"),
            "detail" => Path.Combine(viewPath, "detail.vue"),
            _ => throw new Exception($"不支持的页面类型: {pageType}")
        };
    }

    public async Task<FieldPropertyPanelDto> GetFieldPropertyPanelAsync(string tag)
    {
        var componentTag = tag.StartsWith("el-") ? tag[3..] : tag;

        var component = await _db.Queryable<SysComponent>()
            .Where(c => c.Tag == componentTag && c.Name == componentTag)
            .FirstAsync()
            ?? await _db.Queryable<SysComponent>()
                .Where(c => c.Tag == componentTag)
                .FirstAsync()
            ?? await _db.Queryable<SysComponent>()
                .Where(c => c.Name == componentTag)
                .FirstAsync();

        var result = new FieldPropertyPanelDto
        {
            FieldName = tag,
            ComponentTag = componentTag,
            ComponentName = component?.Name ?? componentTag,
            FormControlType = ""
        };

        List<SysComponentProperty> props;
        List<SysComponentEvent> events;
        List<SysComponentSlot> slots;

        if (component != null)
        {
            props = await _db.Queryable<SysComponentProperty>().Where(p => p.ComponentId == component.Id).OrderBy(p => p.Sort).ToListAsync();
            events = await _db.Queryable<SysComponentEvent>().Where(e => e.ComponentId == component.Id).OrderBy(e => e.Sort).ToListAsync();
            slots = await _db.Queryable<SysComponentSlot>().Where(s => s.ComponentId == component.Id).OrderBy(s => s.Sort).ToListAsync();
        }
        else if (IsHtmlTag(componentTag))
        {
            result.ComponentName = $"HTML <{componentTag}>";
            (props, events, slots) = GetHtmlDefaults(componentTag);
        }
        else
        {
            props = new List<SysComponentProperty>();
            events = new List<SysComponentEvent>();
            slots = new List<SysComponentSlot>();
        }

        var systemProps = new HashSet<string> { "model", "v-model", "prop", "data-gen-id", "ref", "style", "class", "id" };
        foreach (var prop in props.Where(p => !systemProps.Contains(p.PropName)))
        {
            result.Properties.Add(new PropertyItemDto
            {
                PropName = prop.PropName, PropType = prop.PropType,
                EnumValues = prop.EnumValues, DefaultValue = prop.DefaultValue,
                Description = prop.Description, IsCommon = prop.IsCommon,
                IsAdvanced = prop.IsAdvanced, IsActive = false,
                CurrentValue = null, ValueType = "static"
            });
        }

        var directives = await _db.Queryable<SysDirective>().OrderBy(d => d.Sort).ToListAsync();
        foreach (var d in directives)
        {
            result.Directives.Add(new DirectiveItemDto
            {
                DirectiveName = d.DirectiveName, Description = d.Description,
                HasValue = d.HasValue, IsCommon = d.IsCommon,
                IsActive = false, CurrentValue = null
            });
        }

        foreach (var evt in events)
        {
            result.Events.Add(new EventItemDto
            {
                EventName = evt.EventName, Description = evt.Description,
                IsCommon = evt.IsCommon, IsSingle = evt.IsSingle,
                IsActive = false, CurrentValue = null, IsSingleActive = false
            });
        }

        foreach (var s in slots)
        {
            result.Slots.Add(new SlotItemDto
            {
                SlotName = s.SlotName, Description = s.Description,
                IsCommon = s.IsCommon, IsActive = false
            });
        }

        return result;
    }

    private static readonly HashSet<string> HtmlTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "span", "p", "a", "img", "button", "input", "select", "option",
        "textarea", "form", "label", "table", "tr", "td", "th", "thead", "tbody",
        "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6", "section",
        "header", "footer", "nav", "article", "aside", "main", "figure",
        "figcaption", "blockquote", "pre", "code", "hr", "br", "em", "strong",
        "small", "i", "b", "u", "del", "ins", "sub", "sup", "mark", "time",
        "abbr", "address", "dl", "dt", "dd", "fieldset", "legend", "iframe",
        "video", "audio", "canvas", "svg", "path", "template", "slot",
    };

    private static bool IsHtmlTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        // el- 开头的不是裸 HTML
        if (tag.StartsWith("el-", StringComparison.OrdinalIgnoreCase)) return false;
        // 纯 HTML 标签（全小写字母，不带连字符或斜杠）
        if (HtmlTags.Contains(tag)) return true;
        // 未知标签，如果是全小写且不含 : 或 -，当作 HTML 处理
        return tag.All(c => char.IsLower(c) || char.IsDigit(c)) && !tag.Contains(':');
    }

    private static (List<SysComponentProperty>, List<SysComponentEvent>, List<SysComponentSlot>) GetHtmlDefaults(string tag)
    {
        var props = new List<SysComponentProperty>();
        var events = new List<SysComponentEvent>();
        var slots = new List<SysComponentSlot>();

        // ── 通用 HTML 属性 ──
        var commonAttrs = new (string name, string type, string? enumVals, string? defVal, string desc, bool common, bool advanced)[]
        {
            ("id",      "string", null, null, "元素唯一标识", true, false),
            ("class",   "string", null, null, "CSS 类名", true, false),
            ("style",   "string", null, null, "内联样式", true, false),
            ("title",   "string", null, null, "提示文本", true, false),
            ("hidden",  "boolean", null, null, "隐藏元素", false, false),
            ("tabindex","number", null, null, "Tab 键顺序", false, true),
            ("dir",     "enum", "ltr,rtl,auto", null, "文本方向", false, true),
            ("draggable","enum", "true,false,auto", null, "可拖拽", false, true),
            ("contenteditable","enum","true,false",null,"可编辑内容",false,true),
            ("lang",    "string", null, null, "语言代码", false, true),
            ("spellcheck","enum","true,false",null,"拼写检查",false,true),
            ("accesskey","string", null, null, "快捷键", false, true),
            ("data-*",  "string", null, null, "自定义数据属性", false, true),
        };

        // ── 输入类属性 ──
        var inputAttrs = new (string name, string type, string? enumVals, string? defVal, string desc, bool common, bool advanced)[]
        {
            ("type",        "enum", "text,password,email,number,date,checkbox,radio,file,hidden,submit,reset,button", "text", "输入类型", true, false),
            ("name",        "string", null, null, "表单字段名", true, false),
            ("value",       "string", null, null, "默认值", true, false),
            ("placeholder", "string", null, null, "占位提示", true, false),
            ("disabled",    "boolean", null, null, "禁用", true, false),
            ("readonly",    "boolean", null, null, "只读", false, false),
            ("required",    "boolean", null, null, "必填", false, false),
            ("maxlength",   "number", null, null, "最大长度", false, false),
            ("minlength",   "number", null, null, "最小长度", false, true),
            ("min",         "number", null, null, "最小值", false, true),
            ("max",         "number", null, null, "最大值", false, true),
            ("step",        "number", null, null, "步长", false, true),
            ("pattern",     "string", null, null, "正则校验", false, true),
            ("autocomplete","enum","on,off",null,"自动补全",false,true),
            ("autofocus",   "boolean", null, null, "自动聚焦", false, true),
            ("multiple",    "boolean", null, null, "多选/多文件", false, true),
            ("checked",     "boolean", null, null, "选中状态", false, true),
        };

        // ── 链接/媒体属性 ──
        var linkMediaAttrs = new (string name, string type, string? enumVals, string? defVal, string desc, bool common, bool advanced)[]
        {
            ("href",   "string", null, null, "链接地址", true, false),
            ("target", "enum", "_self,_blank,_parent,_top", "_self", "打开方式", true, false),
            ("rel",    "string", null, null, "链接关系", false, true),
            ("src",    "string", null, null, "资源地址", true, false),
            ("alt",    "string", null, null, "替代文本", true, false),
            ("width",  "string", null, null, "宽度", false, false),
            ("height", "string", null, null, "高度", false, false),
        };

        // ── 事件 ──
        var htmlEvents = new (string name, string? desc, bool common, bool isSingle)[]
        {
            ("click",     "鼠标点击", true, false),
            ("dblclick",  "鼠标双击", false, false),
            ("mousedown", "鼠标按下", false, true),
            ("mouseup",   "鼠标松开", false, true),
            ("mouseover", "鼠标进入", false, true),
            ("mouseout",  "鼠标离开", false, true),
            ("mousemove", "鼠标移动", false, true),
            ("keydown",   "键盘按下", false, false),
            ("keyup",     "键盘松开", false, false),
            ("keypress",  "键盘按键", false, false),
            ("focus",     "获得焦点", false, false),
            ("blur",      "失去焦点", false, false),
            ("change",    "值改变", true, false),
            ("input",     "输入时", true, false),
            ("submit",    "表单提交", false, false),
            ("reset",     "表单重置", false, true),
            ("scroll",    "滚动时", false, true),
            ("resize",    "大小改变", false, true),
            ("load",      "加载完成", false, true),
            ("error",     "加载出错", false, true),
        };

        // 添加通用属性
        foreach (var a in commonAttrs)
            props.Add(MakeProp(a.name, a.type, a.enumVals, a.defVal, a.desc, a.common, a.advanced));

        // 根据标签类型添加特定属性
        var tagLower = tag.ToLower();
        if (tagLower is "input" or "textarea" or "select")
        {
            foreach (var a in inputAttrs)
                props.Add(MakeProp(a.name, a.type, a.enumVals, a.defVal, a.desc, a.common, a.advanced));
        }
        if (tagLower is "a" or "link" or "img" or "video" or "audio" or "iframe" or "source")
        {
            foreach (var a in linkMediaAttrs)
                props.Add(MakeProp(a.name, a.type, a.enumVals, a.defVal, a.desc, a.common, a.advanced));
        }
        if (tagLower is "img")
        {
            props.Add(MakeProp("loading", "enum", "lazy,eager,auto", "auto", "懒加载", false, false));
        }

        // 添加事件
        foreach (var e in htmlEvents)
        {
            events.Add(new SysComponentEvent
            {
                EventName = e.name,
                Description = e.desc,
                IsCommon = e.common,
                IsSingle = e.isSingle,
                Sort = 0
            });
        }

        return (props, events, slots);
    }

    private static SysComponentProperty MakeProp(string name, string type, string? enumVals, string? defVal, string desc, bool common, bool advanced)
    {
        return new SysComponentProperty
        {
            PropName = name,
            PropType = type,
            EnumValues = enumVals,
            DefaultValue = defVal,
            Description = desc,
            IsCommon = common,
            IsAdvanced = advanced,
            Sort = 0
        };
    }

    private static string MapFormControlToTag(string formControlType)
    {
        return formControlType switch
        {
            "input" => "input",
            "textarea" => "input",
            "number" => "input-number",
            "select" => "select",
            "select-table" => "select",
            "switch" => "switch",
            "date" => "date-picker",
            "datetime" => "date-picker",
            "editor" => "div",  // wangEditor, container is div
            "image" => "upload",
            "file" => "upload",
            "cascader" => "cascader",
            _ => "input"
        };
    }

    /// <summary>
    /// 从 .vue 文件中提取 <template>...</template> 内部内容
    /// </summary>
    private static string ExtractStyleContent(string fullContent)
    {
        var startMatch = global::System.Text.RegularExpressions.Regex.Match(fullContent, @"<style[^>]*>", global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var endMatches = global::System.Text.RegularExpressions.Regex.Matches(fullContent, @"</style>", global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (startMatch.Success && endMatches.Count > 0)
        {
            int start = startMatch.Index + startMatch.Length;
            int end = endMatches[endMatches.Count - 1].Index;
            if (end > start)
            {
                var style = fullContent.Substring(start, end - start).Trim();
                // 去掉 scoped 的 [data-v-xxxxx] 选择器，设计器中不存在这些属性
                style = global::System.Text.RegularExpressions.Regex.Replace(style, @"\[data-v-[^\]]+\]", "");
                return style;
            }
        }
        return string.Empty;
    }

    private static string ExtractTemplateContent(string fullContent)
    {
        var bounds = FindOuterTemplateBounds(fullContent);
        if (bounds != null)
        {
            var (contentStart, contentEnd, _) = bounds.Value;
            if (contentEnd > contentStart)
                return fullContent.Substring(contentStart, contentEnd - contentStart);
        }
        return fullContent;
    }

    /// <summary>
    /// 替换 .vue 文件中 <template> 部分，保留 script 和 style
    /// </summary>
    private static string ReplaceTemplateContent(string fullContent, string newTemplateHtml)
    {
        var bounds = FindOuterTemplateBounds(fullContent);
        if (bounds != null)
        {
            var (start, end, _) = bounds.Value;
            var before = fullContent.Substring(0, start);
            var after = fullContent.Substring(end);
            var indent = DetectIndent(fullContent, start);
            return before + Environment.NewLine + indent + newTemplateHtml.Trim() + Environment.NewLine + after;
        }
        return fullContent;
    }

    private static (int ContentStart, int ContentEnd, int EndTagEnd)? FindOuterTemplateBounds(string fullContent)
    {
        if (string.IsNullOrEmpty(fullContent)) return null;

        var startMatch = global::System.Text.RegularExpressions.Regex.Match(
            fullContent,
            @"<template\b[^>]*>",
            global::System.Text.RegularExpressions.RegexOptions.IgnoreCase | global::System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!startMatch.Success) return null;

        var contentStart = startMatch.Index + startMatch.Length;
        var nextBlockMatch = global::System.Text.RegularExpressions.Regex.Match(
            fullContent.Substring(contentStart),
            @"<(script|style)\b",
            global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var searchEnd = nextBlockMatch.Success
            ? contentStart + nextBlockMatch.Index
            : fullContent.Length;

        var templateRegion = fullContent.Substring(contentStart, searchEnd - contentStart);
        var endMatches = global::System.Text.RegularExpressions.Regex.Matches(
            templateRegion,
            @"</template>",
            global::System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (endMatches.Count == 0) return null;

        var lastEndMatch = endMatches[endMatches.Count - 1];
        var contentEnd = contentStart + lastEndMatch.Index;
        return (contentStart, contentEnd, contentEnd + lastEndMatch.Length);
    }

    private static string DetectIndent(string content, int templateEnd)
    {
        // 简单检测：在 template 开始标签之前的缩进 + 2 空格
        var lineStart = content.LastIndexOf('\n', templateEnd);
        if (lineStart < 0) return "  ";
        var indent = "";
        for (int i = lineStart + 1; i < templateEnd && content[i] == ' '; i++)
            indent += ' ';
        return indent.Length > 0 ? indent + "  " : "  ";
    }

    private static void DeleteLegacyPageAutoJs(string viewPath, string pageType, string entityLower)
    {
        var legacyPath = Path.Combine(viewPath, $"{pageType}.auto.js");
        var currentPath = Path.Combine(viewPath, $"{entityLower}.{pageType}.auto.js");

        if (string.Equals(Path.GetFullPath(legacyPath), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(legacyPath))
            File.Delete(legacyPath);
    }

    private async Task DeleteChildGeneratedFilesAsync(ModuleEntity parentEntity, string childEntityName)
    {
        if (string.IsNullOrWhiteSpace(childEntityName))
            return;

        var parentIndexPath = await ResolveVueFilePath(parentEntity, "index");
        var viewPath = Path.GetDirectoryName(parentIndexPath);
        if (string.IsNullOrWhiteSpace(viewPath) || !Directory.Exists(viewPath))
            return;

        var childLower = ToLowerCamel(childEntityName.Trim());
        var patterns = new[]
        {
            $"{childLower}.*.auto.js",
            $"{childLower}.*.script.json",
            $"{childLower}.*.fields.json"
        };

        foreach (var pattern in patterns)
        {
            foreach (var filePath in Directory.GetFiles(viewPath, pattern, SearchOption.TopDirectoryOnly))
                File.Delete(filePath);
        }
    }

    private async Task CleanupChildVueReferencesAsync(ModuleEntity parentEntity, string childEntityName)
    {
        if (string.IsNullOrWhiteSpace(childEntityName))
            return;

        var parentIndexPath = await ResolveVueFilePath(parentEntity, "index");
        var viewPath = Path.GetDirectoryName(parentIndexPath);
        if (string.IsNullOrWhiteSpace(viewPath) || !Directory.Exists(viewPath))
            return;

        foreach (var vuePath in Directory.GetFiles(viewPath, "*.vue", SearchOption.TopDirectoryOnly))
        {
            var pageType = Path.GetFileNameWithoutExtension(vuePath);
            var content = await File.ReadAllTextAsync(vuePath);
            var updated = RemoveChildScriptSetupReferences(content, childEntityName.Trim(), pageType);
            if (!string.Equals(content, updated, StringComparison.Ordinal))
                await File.WriteAllTextAsync(vuePath, updated);
        }
    }

    private static string RemoveChildScriptSetupReferences(string content, string childEntityName, string pageType)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(childEntityName) || string.IsNullOrWhiteSpace(pageType))
            return content;

        var childLower = global::System.Text.RegularExpressions.Regex.Escape(ToLowerCamel(childEntityName));
        var childPascal = global::System.Text.RegularExpressions.Regex.Escape(ToPascalCase(childEntityName));
        var safePageType = global::System.Text.RegularExpressions.Regex.Escape(pageType);

        var importPattern = @"^[\t ]*import\s+\{\s*use" + childPascal + @"Child\s*\}\s+from\s+['""]\./" +
            childLower + @"\." + safePageType + @"\.auto\.js['""];?[\t ]*(?:\r?\n|$)";
        var callPattern = @"^[\t ]*const\s+\{[^}]*\}\s*=\s*use" + childPascal + @"Child\s*\([^;]*\);[\t ]*(?:\r?\n|$)";

        var updated = global::System.Text.RegularExpressions.Regex.Replace(
            content,
            importPattern,
            string.Empty,
            global::System.Text.RegularExpressions.RegexOptions.Multiline);
        updated = global::System.Text.RegularExpressions.Regex.Replace(
            updated,
            callPattern,
            string.Empty,
            global::System.Text.RegularExpressions.RegexOptions.Multiline);

        return updated;
    }

    private static string ToLowerCamel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// 设计器删除子表节点时联动删除 OneToManyRelation
    /// </summary>
    public async Task<bool> DeleteChildRelationAsync(long entityId, string childEntityName)
    {
        if (!_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
            return false;

        var relation = await _db.Queryable<OneToManyRelation>()
            .Where(r => r.ModuleEntityId == entityId && r.ChildEntityName == childEntityName)
            .FirstAsync();

        if (relation == null) return false;

        await _db.Updateable<OneToManyRelation>()
            .SetColumns(r => r.IsDeleted == true)
            .SetColumns(r => r.DeleteTime == DateTime.UtcNow)
            .Where(r => r.Id == relation.Id)
            .ExecuteCommandAsync();

        // 取消子表引用标记
        await _db.Updateable<ModuleEntity>()
            .SetColumns(e => e.IsChildTable == false)
            .Where(e => e.Id == relation.ChildEntityId)
            .ExecuteCommandAsync();

        var parentEntity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == relation.ModuleEntityId)
            .FirstAsync();
        if (parentEntity != null)
        {
            var removedChildName = relation.ChildEntityName ?? childEntityName;
            await CleanupChildVueReferencesAsync(parentEntity, removedChildName);
            await DeleteChildGeneratedFilesAsync(parentEntity, removedChildName);
        }

        return true;
    }

    /// <summary>
    /// 测试模板生成器（直接输出到临时文件）
    /// </summary>
    public async Task<string> TestTemplateGenerateAsync(long id, string pageType)
    {
        var gen = new TemplateCodeGenerator(_db);
        var result = await gen.GeneratePageAsync(id, pageType, "TestProject", "test");
        var scriptBuilder = new StringBuilder();
        scriptBuilder.AppendLine(result.MainScriptContent);
        foreach (var childScript in result.ChildScripts.Values)
            scriptBuilder.AppendLine(childScript.ScriptContent);
        return BuildVueFileContent(result.VueContent, scriptBuilder.ToString(), pageType);
    }

    public async Task<List<SysPageTemplate>> GetPageTemplatesAsync()
    {
        return await _db.Queryable<SysPageTemplate>().Where(t => t.IsDeleted == false).OrderBy(t => t.PageType).ToListAsync();
    }

    public async Task<bool> SavePageTemplateAsync([FromBody] SysPageTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.Id <= 0)
        {
            template.Id = YitIdHelper.NextId();
            template.CreateTime = DateTime.UtcNow;
        }

        template.UpdateTime = DateTime.UtcNow;
        var exists = await _db.Queryable<SysPageTemplate>().Where(t => t.Id == template.Id).AnyAsync();
        if (exists)
            await _db.Updateable(template).ExecuteCommandAsync();
        else
            await _db.Insertable(template).ExecuteCommandAsync();
        return true;
    }

    public async Task<bool> SaveFieldControlTemplateAsync([FromBody] SysFieldControlTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.Id <= 0)
        {
            template.Id = YitIdHelper.NextId();
            template.CreateTime = DateTime.UtcNow;
        }

        template.UpdateTime = DateTime.UtcNow;
        var exists = await _db.Queryable<SysFieldControlTemplate>().Where(t => t.Id == template.Id).AnyAsync();
        if (exists)
            await _db.Updateable(template).ExecuteCommandAsync();
        else
            await _db.Insertable(template).ExecuteCommandAsync();
        return true;
    }

    public async Task<List<SysFieldControlTemplate>> GetFieldControlTemplatesAsync()
    {
        return await _db.Queryable<SysFieldControlTemplate>().Where(t => t.IsDeleted == false).OrderBy(t => t.ControlType).ToListAsync();
    }

    public async Task<List<SysChildTemplate>> GetChildTemplatesAsync()
    {
        return await _db.Queryable<SysChildTemplate>().Where(t => t.IsDeleted == false).OrderBy(t => t.PageType).ToListAsync();
    }

    public async Task<bool> SaveChildTemplateAsync([FromBody] SysChildTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.Id <= 0)
        {
            template.Id = YitIdHelper.NextId();
            template.CreateTime = DateTime.UtcNow;
        }

        template.UpdateTime = DateTime.UtcNow;
        var exists = await _db.Queryable<SysChildTemplate>().Where(t => t.Id == template.Id).AnyAsync();
        if (exists)
            await _db.Updateable(template).ExecuteCommandAsync();
        else
            await _db.Insertable(template).ExecuteCommandAsync();
        return true;
    }

    private void FillTenantInfo(CodeMaster.Core.Entities.ITenant entity)
    {
        var currentTenantId = _tenantContext?.CurrentTenantId;
        if (currentTenantId.HasValue && currentTenantId.Value > 0)
        {
            entity.TenantId = currentTenantId.Value;
            return;
        }

        if (entity.TenantId == 0)
        {
            entity.TenantId = currentTenantId ?? 0;
        }
    }

}
