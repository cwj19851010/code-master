using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 页面模板种子数据（页面主模板 + 子表模板 + 控件模板）
/// </summary>
public class TemplateModule : ISeedModule
{
    public string ModuleName => "页面模板";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<SysPageTemplate>().AnyAsync();
    }

    public Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 模板管理不需要菜单，随代码生成使用
        return Task.CompletedTask;
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        var hasData = await dbContext.Set<SysPageTemplate>().AnyAsync();
        if (hasData)
        {
            Console.WriteLine("  - 页面模板数据已存在，跳过");
            return;
        }

        var now = DateTime.UtcNow;
        var emptyScript = JsonSerializer.Serialize(new { imports = new object[0], uses = new object[0], refs = new object[0], reactives = new object[0], functions = new object[0], hooks = new object[0], computed = new object[0], watches = new object[0] });

        // 控件级 ScriptSection JSON（Marker 格式，生成时被 ScriptBuilder.ScriptSection.FromMarker 转换）
        var editorScript = GetEditorScript();
        var selectDictScript = GetSelectDictScript();
        var selectTableScript = GetSelectTableScript();
        var fileScript = GetFileScript();
        var imageScript = GetImageScript();

        // === Page Templates ===
        var pages = new[]
        {
            new { Type = "index", Name = "列表页", Html = GetIndexTemplate(), Script = GetIndexScript(), Sort = 1 },
            new { Type = "add", Name = "新增页", Html = GetAddTemplate(), Script = GetAddScript(), Sort = 2 },
            new { Type = "edit", Name = "编辑页", Html = GetEditTemplate(), Script = GetEditScript(), Sort = 3 },
            new { Type = "detail", Name = "详情页", Html = GetDetailTemplate(), Script = GetDetailScript(), Sort = 4 },
        };

        foreach (var p in pages)
        {
            await dbContext.Set<SysPageTemplate>().AddAsync(new SysPageTemplate
            {
                Id = YitIdHelper.NextId(),
                PageType = p.Type,
                Name = p.Name,
                HtmlContent = p.Html,
                ScriptSections = p.Script,
                IsSystem = true,
                Sort = p.Sort,
                CreateTime = now,
            });
        }
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"  ✓ 导入 {pages.Length} 个页面主模板");

        // === Child Templates ===
        var cardHtml = "<el-card shadow=\"never\" style=\"margin-top:20px\" data-gen-id=\"gen_child_[relation.entityName]\"><template #header><div class=\"card-header\" style=\"display:flex;align-items:center;justify-content:space-between\"><span>{{ $t('[relation.entityTitleKey]') }}</span><el-button data-gen-id=\"gen_child_action_add_[relation.entityName]\" type=\"primary\" size=\"small\" @click=\"handleAdd[relation.entityName]\">{{ $t('add') }}</el-button></div></template><el-table :data=\"(form.[relation.collectionName] || []).filter(i => i.rowStatus !== 3)\" border stripe>[relation.tableColumns]<el-table-column :label=\"$t('operation')\" width=\"140\"><template #default=\"scope\"><el-button data-gen-id=\"gen_child_action_edit_[relation.entityName]\" link type=\"primary\" size=\"small\" @click=\"handleEdit[relation.entityName](scope.row, scope.$index)\">{{ $t('edit') }}</el-button><el-button data-gen-id=\"gen_child_action_remove_[relation.entityName]\" link type=\"danger\" size=\"small\" @click=\"handleRemove[relation.entityName](scope.$index)\">{{ $t('delete') }}</el-button></template></el-table-column></el-table></el-card>";
        var cardScript = GetChildCardScript();
        var dialogHtml = "<el-dialog v-model=\"[relation.dialogVisibleName]\" :title=\"$t('[relation.entityTitleKey]') + ([relation.editingIndexName] >= 0 ? $t('edit') : $t('add'))\" width=\"700px\"><el-form ref=\"[relation.formRefName]\" :model=\"[relation.formName]\" label-width=\"100px\"><el-row :gutter=\"16\">[relation.dialogColumns]</el-row></el-form><template #footer><el-button data-gen-id=\"gen_child_action_cancel_[relation.entityName]\" @click=\"[relation.dialogVisibleName] = false\">{{ $t('cancel') }}</el-button><el-button data-gen-id=\"gen_child_action_submit_[relation.entityName]\" type=\"primary\" @click=\"handle[relation.entityName]Submit\">{{ $t('confirm') }}</el-button></template></el-dialog>";

        foreach (var pt in new[] { "add", "edit" })
        {
            await dbContext.Set<SysChildTemplate>().AddAsync(new SysChildTemplate { Id = YitIdHelper.NextId(), PageType = pt, ChildType = "card", HtmlContent = cardHtml, ScriptSections = cardScript, Sort = 1, CreateTime = now });
            await dbContext.Set<SysChildTemplate>().AddAsync(new SysChildTemplate { Id = YitIdHelper.NextId(), PageType = pt, ChildType = "dialog", HtmlContent = dialogHtml, ScriptSections = emptyScript, Sort = 2, CreateTime = now });
        }

        // 详情页子表（只读表格）
        var detailChildHtml = "<el-card shadow=\"never\" style=\"margin-top:20px\" data-gen-id=\"gen_child_[relation.entityName]\"><template #header><div class=\"card-header\"><span>{{ $t('[relation.entityTitleKey]') }}</span></div></template><el-table :data=\"detail.[relation.collectionName] || []\" border stripe>[relation.tableColumns]</el-table></el-card>";
        await dbContext.Set<SysChildTemplate>().AddAsync(new SysChildTemplate { Id = YitIdHelper.NextId(), PageType = "detail", ChildType = "card", HtmlContent = detailChildHtml, ScriptSections = emptyScript, Sort = 1, CreateTime = now });

        Console.WriteLine("  ✓ 导入 5 个子表模板（含详情页）");

        // === Control Templates ===
        var ctrls = GetControlTemplates(emptyScript, selectDictScript, selectTableScript, editorScript, fileScript, imageScript);
        foreach (var c in ctrls)
        {
            await dbContext.Set<SysFieldControlTemplate>().AddAsync(new SysFieldControlTemplate
            {
                Id = YitIdHelper.NextId(),
                ControlType = c.Type,
                PageSection = c.Section,
                HtmlContent = c.Html,
                ScriptSections = c.Script,
                Sort = c.Sort,
                CreateTime = now,
            });
        }
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"  ✓ 导入 {ctrls.Length} 个控件模板");
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>();
    }

    // === Template content ===

    private static string GetIndexTemplate() => "<div class=\"app-container\">\n  <el-card shadow=\"never\" class=\"mb-20\" data-gen-id=\"gen_search_area\">\n    <el-form ref=\"queryFormRef\" :model=\"queryParams\" inline class=\"query-form\">\n      [gen.searchColumns]\n      <el-form-item>\n        <el-button data-gen-id=\"gen_action_search\" type=\"primary\" @click=\"handleSearch\">{{ $t('search') }}</el-button>\n        <el-button data-gen-id=\"gen_action_reset\" @click=\"handleReset\">{{ $t('reset') }}</el-button>\n      </el-form-item>\n    </el-form>\n  </el-card>\n\n  <el-card shadow=\"never\" data-gen-id=\"gen_list_area\">\n    <div class=\"toolbar\" data-gen-id=\"gen_toolbar\">\n      <el-button data-gen-id=\"gen_action_add\" type=\"primary\" @click=\"handleAdd\">{{ $t('add') }}</el-button>\n    </div>\n\n    <el-table :data=\"tableData\" border stripe v-loading=\"loading\">\n      <el-table-column type=\"selection\" width=\"50\" />\n      [gen.listColumns]\n      <el-table-column :label=\"$t('operation')\" width=\"220\" fixed=\"right\" data-gen-id=\"gen_operations\">\n        <template #default=\"scope\">\n          <el-button data-gen-id=\"gen_action_detail\" link type=\"primary\" @click=\"handleDetail(scope.row)\">{{ $t('detail') }}</el-button>\n          <el-button data-gen-id=\"gen_action_edit\" link type=\"primary\" @click=\"handleEdit(scope.row)\">{{ $t('edit') }}</el-button>\n          <el-button data-gen-id=\"gen_action_delete\" link type=\"danger\" @click=\"handleDelete(scope.row)\">{{ $t('delete') }}</el-button>\n        </template>\n      </el-table-column>\n    </el-table>\n\n    <el-pagination\n      data-gen-id=\"gen_pagination\"\n      v-model:current-page=\"pagination.page\"\n      v-model:page-size=\"pagination.pageSize\"\n      :total=\"pagination.total\"\n      :page-sizes=\"[10, 20, 50, 100]\"\n      layout=\"total, sizes, prev, pager, next, jumper\"\n      @size-change=\"getList\"\n      @current-change=\"getList\" />\n  </el-card>\n</div>";

    private static string GetIndexScript() => "{ \"imports\":[{\"path\":\"@/api/[gen.moduleNameLower]/[gen.entityNameLower]\",\"default\":\"* as [gen.entityNameLower]Api\"},{\"path\":\"vue\",\"destructured\":\"ref, reactive, onMounted, onActivated\"},{\"path\":\"vue-router\",\"destructured\":\"useRouter\"},{\"path\":\"element-plus\",\"destructured\":\"ElMessage, ElMessageBox\"}],\"reactives\":[{\"name\":\"queryParams\",\"fields\":{}},{\"name\":\"pagination\",\"fields\":{\"page\":\"1\",\"pageSize\":\"10\",\"total\":\"0\"}}],\"refs\":[{\"name\":\"loading\",\"initialValue\":\"false\"},{\"name\":\"tableData\",\"initialValue\":\"[]\"},{\"name\":\"router\",\"initialValue\":\"useRouter()\"},{\"name\":\"hasActivated\",\"initialValue\":\"false\"}],\"functions\":[{\"name\":\"getList\",\"params\":\"\",\"async\":true,\"body\":[\"loading.value = true;\",\"try {\",\"  const params = { ...queryParams, pageNum: pagination.page, pageSize: pagination.pageSize };\",\"  const res = await [gen.entityNameLower]Api.getPagedList(params);\",\"  tableData.value = res.items;\",\"  pagination.total = res.total;\",\"}\",\"catch(e) { ElMessage.error(e.message); }\",\"finally { loading.value = false; }\"]},{\"name\":\"handleSearch\",\"params\":\"\",\"async\":true,\"body\":[\"pagination.page = 1;\",\"await getList();\"]},{\"name\":\"handleReset\",\"params\":\"\",\"async\":true,\"body\":[\"Object.keys(queryParams).forEach(k => delete queryParams[k]);\",\"await handleSearch();\"]},{\"name\":\"handleAdd\",\"params\":\"\",\"body\":[\"router.push('/[gen.moduleNameLower]/[gen.entityNameLower]/add');\"]},{\"name\":\"handleDetail\",\"params\":\"row\",\"body\":[\"router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/detail?id=${row.id}`);\"]},{\"name\":\"handleEdit\",\"params\":\"row\",\"body\":[\"router.push(`/[gen.moduleNameLower]/[gen.entityNameLower]/edit?id=${row.id}`);\"]},{\"name\":\"handleDelete\",\"params\":\"row\",\"body\":[\"ElMessageBox.confirm('确定删除吗？', '提示', { type: 'warning' }).then(async () => {\",\"  await [gen.entityNameLower]Api.deleteById(row.id);\",\"  ElMessage.success('删除成功');\",\"  await getList();\",\"});\"]},{\"name\":\"getDictLabel\",\"params\":\"val, options\",\"body\":[\"if (val === undefined || val === null) return '-'\",\"if (!options) return val\",\"const item = options.find(o => String(o.value) === String(val))\",\"return item?.label ?? val\"]},{\"name\":\"getSelectLabel\",\"params\":\"val, options, field\",\"body\":[\"if (val === undefined || val === null || val === '') return '-'\",\"if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val\",\"const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)\",\"const pick = (item, f) => item[f]\",\"const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }\",\"if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')\",\"if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')\",\"return resolve(val)\"]},{\"name\":\"formatDate\",\"params\":\"val, withTime\",\"body\":[\"if (!val) return '-'\",\"const d = new Date(val)\",\"const pad = (n) => String(n).padStart(2, '0')\",\"const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`\",\"return withTime ? `${date} ${pad(d.getHours())}:${pad(d.getMinutes())}` : date\"]}],\"hooks\":[{\"name\":\"onMounted\",\"body\":[\"getList();\"]},{\"name\":\"onActivated\",\"body\":[\"if (hasActivated.value) { getList(); }\",\"hasActivated.value = true;\"]}]}";

    private static string GetAddTemplate() => "<div class=\"app-container\">\n  <el-card shadow=\"never\" data-gen-id=\"gen_form_area\">\n    <template #header>\n      <div class=\"card-header\"><span>{{ $t('add') }} {{ $t('[gen.entityTitleKey]') }}</span></div>\n    </template>\n    <el-form ref=\"formRef\" :model=\"form\" :rules=\"rules\" label-width=\"120px\">\n      <el-row :gutter=\"20\">\n        [gen.addColumns]\n      </el-row>\n    </el-form>\n  </el-card>\n[gen.relationCards]\n    <div style=\"margin-top:20px;text-align:center\" data-gen-id=\"gen_form_actions\">\n      <el-button data-gen-id=\"gen_action_submit\" type=\"primary\" @click=\"handleSubmit\">{{ $t('save') }}</el-button>\n      <el-button data-gen-id=\"gen_action_cancel\" @click=\"handleCancel\">{{ $t('cancel') }}</el-button>\n    </div>\n[gen.relationDialogs]\n</div>";

    private static string GetAddScript() => "{ \"imports\":[{\"path\":\"@/api/[gen.moduleNameLower]/[gen.entityNameLower]\",\"default\":\"* as [gen.entityNameLower]Api\"},{\"path\":\"vue\",\"destructured\":\"ref, reactive, onMounted\"},{\"path\":\"vue-router\",\"destructured\":\"useRouter\"},{\"path\":\"element-plus\",\"destructured\":\"ElMessage\"}],\"refs\":[{\"name\":\"formRef\"},{\"name\":\"router\",\"initialValue\":\"useRouter()\"}],\"reactives\":[{\"name\":\"form\",\"fields\":{}},{\"name\":\"rules\",\"fields\":{}}],\"functions\":[{\"name\":\"handleSubmit\",\"params\":\"\",\"async\":true,\"body\":[\"if (!formRef.value) return;\",\"await formRef.value.validate(async (valid) => {\",\"  if (valid) {\",\"    try { await [gen.entityNameLower]Api.create(form); ElMessage.success('\u4fdd\u5b58\u6210\u529f'); formRef.value?.resetFields(); router.go(-1); }\",\"    catch(e) { ElMessage.error(e.message); }\",\"  }\",\"});\"]},{\"name\":\"handleCancel\",\"params\":\"\",\"body\":[\"router.go(-1);\"]},{\"name\":\"getSelectLabel\",\"params\":\"val, options, field\",\"body\":[\"if (val === undefined || val === null || val === '') return '-'\",\"if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val\",\"const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)\",\"const pick = (item, f) => item[f]\",\"const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }\",\"if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')\",\"if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')\",\"return resolve(val)\"]}]}";

    private static string GetEditTemplate() => "<div class=\"app-container\">\n  <el-card shadow=\"never\" data-gen-id=\"gen_form_area\">\n    <template #header>\n      <div class=\"card-header\"><span>{{ $t('edit') }} {{ $t('[gen.entityTitleKey]') }}</span></div>\n    </template>\n    <el-form ref=\"formRef\" :model=\"form\" :rules=\"rules\" label-width=\"120px\">\n      <el-row :gutter=\"20\">\n        [gen.editColumns]\n      </el-row>\n    </el-form>\n  </el-card>\n[gen.relationCards]\n    <div style=\"margin-top:20px;text-align:center\" data-gen-id=\"gen_form_actions\">\n      <el-button data-gen-id=\"gen_action_submit\" type=\"primary\" @click=\"handleSubmit\">{{ $t('save') }}</el-button>\n      <el-button data-gen-id=\"gen_action_cancel\" @click=\"handleCancel\">{{ $t('cancel') }}</el-button>\n    </div>\n[gen.relationDialogs]\n</div>";

    private static string GetEditScript() => "{ \"imports\":[{\"path\":\"@/api/[gen.moduleNameLower]/[gen.entityNameLower]\",\"default\":\"* as [gen.entityNameLower]Api\"},{\"path\":\"vue\",\"destructured\":\"ref, reactive, onMounted\"},{\"path\":\"vue-router\",\"destructured\":\"useRouter, useRoute\"},{\"path\":\"element-plus\",\"destructured\":\"ElMessage\"}],\"refs\":[{\"name\":\"formRef\"},{\"name\":\"router\",\"initialValue\":\"useRouter()\"},{\"name\":\"route\",\"initialValue\":\"useRoute()\"}],\"reactives\":[{\"name\":\"form\",\"fields\":{}},{\"name\":\"rules\",\"fields\":{}}],\"functions\":[{\"name\":\"getDetail\",\"params\":\"\",\"async\":true,\"body\":[\"const id = route.query.id;\",\"if (id) { const res = await [gen.entityNameLower]Api.getById(id); Object.assign(form, res); }\"]},{\"name\":\"handleSubmit\",\"params\":\"\",\"async\":true,\"body\":[\"if (!formRef.value) return;\",\"await formRef.value.validate(async (valid) => {\",\"  if (valid) {\",\"    try { await [gen.entityNameLower]Api.update(form.id, form); ElMessage.success('保存成功'); router.go(-1); }\",\"    catch(e) { ElMessage.error(e.message); }\",\"  }\",\"});\"]},{\"name\":\"handleCancel\",\"params\":\"\",\"body\":[\"router.go(-1);\"]},{\"name\":\"getSelectLabel\",\"params\":\"val, options, field\",\"body\":[\"if (val === undefined || val === null || val === '') return '-'\",\"if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val\",\"const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)\",\"const pick = (item, f) => item[f]\",\"const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }\",\"if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')\",\"if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')\",\"return resolve(val)\"]}],\"hooks\":[{\"name\":\"onMounted\",\"body\":[\"getDetail();\"]}]}";

    private static string GetDetailTemplate() => "<div class=\"app-container\">\n  <el-card shadow=\"never\" data-gen-id=\"gen_detail_area\">\n    <template #header>\n      <div class=\"card-header\"><span>{{ $t('[gen.entityTitleKey]') }} {{ $t('detail') }}</span></div>\n    </template>\n    <el-descriptions :column=\"2\" border>\n      [gen.detailColumns]\n    </el-descriptions>\n  </el-card>\n[gen.relationCards]\n    <div style=\"margin-top:20px;text-align:center\" data-gen-id=\"gen_detail_actions\">\n      <el-button data-gen-id=\"gen_action_back\" @click=\"handleBack\">{{ $t('back') }}</el-button>\n    </div>\n</div>";

    private static string GetDetailScript() => "{ \"imports\":[{\"path\":\"@/api/[gen.moduleNameLower]/[gen.entityNameLower]\",\"default\":\"* as [gen.entityNameLower]Api\"},{\"path\":\"vue\",\"destructured\":\"ref, reactive, onMounted\"},{\"path\":\"vue-router\",\"destructured\":\"useRouter, useRoute\"}],\"refs\":[{\"name\":\"router\",\"initialValue\":\"useRouter()\"},{\"name\":\"route\",\"initialValue\":\"useRoute()\"}],\"reactives\":[{\"name\":\"detail\",\"fields\":{}}],\"functions\":[{\"name\":\"getDetail\",\"params\":\"\",\"async\":true,\"body\":[\"const id = route.query.id;\",\"if (id) { const res = await [gen.entityNameLower]Api.getById(id); Object.assign(detail, res); }\"]},{\"name\":\"handleBack\",\"params\":\"\",\"body\":[\"router.go(-1);\"]},{\"name\":\"getSelectLabel\",\"params\":\"val, options, field\",\"body\":[\"if (val === undefined || val === null || val === '') return '-'\",\"if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val\",\"const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)\",\"const pick = (item, f) => item[f]\",\"const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }\",\"if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')\",\"if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')\",\"return resolve(val)\"]},{\"name\":\"formatDate\",\"params\":\"val, withTime\",\"body\":[\"if (!val) return '-'\",\"const d = new Date(val)\",\"const pad = (n) => String(n).padStart(2, '0')\",\"const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`\",\"return withTime ? `${date} ${pad(d.getHours())}:${pad(d.getMinutes())}` : date\"]}],\"hooks\":[{\"name\":\"onMounted\",\"body\":[\"getDetail();\"]}]}";

    private static string GetChildCardScript() => "{ \"imports\":[{\"path\":\"vue\",\"destructured\":\"ref, reactive, onMounted\"}],\"functions\":[{\"name\":\"handleAdd[relation.entityName]\",\"params\":\"\",\"body\":[\"[relation.editingIndexName].value = -1;\",\"Object.keys([relation.formName]).forEach(k => {\",\"  if (typeof [relation.formName][k] === 'string') [relation.formName][k] = '';\",\"  else if (typeof [relation.formName][k] === 'number') [relation.formName][k] = 0;\",\"  else [relation.formName][k] = null;\",\"});\",\"[relation.formName].rowStatus = 1; // Added\",\"[relation.dialogVisibleName].value = true;\"]},{\"name\":\"handleEdit[relation.entityName]\",\"params\":\"row, index\",\"body\":[\"[relation.editingIndexName].value = index;\",\"Object.assign([relation.formName], row);\",\"[relation.dialogVisibleName].value = true;\"]},{\"name\":\"handleRemove[relation.entityName]\",\"params\":\"index\",\"body\":[\"if (!form || !form.[relation.collectionName]) return;\",\"const item = form.[relation.collectionName][index];\",\"if (!item.id) { form.[relation.collectionName].splice(index, 1); }\",\"else { item.rowStatus = 3; }\"]},{\"name\":\"handle[relation.entityName]Submit\",\"params\":\"\",\"async\":true,\"body\":[\"if (![relation.formRefName].value) return;\",\"if (!form || !form.[relation.collectionName]) { [relation.dialogVisibleName].value = false; return; }\",\"await [relation.formRefName].value.validate(async (valid) => {\",\"  if (valid) {\",\"    if ([relation.editingIndexName].value >= 0) {\",\"      Object.assign(form.[relation.collectionName][[relation.editingIndexName].value], [relation.formName]);\",\"      form.[relation.collectionName][[relation.editingIndexName].value].rowStatus = 2; // Modified\",\"    } else {\",\"      form.[relation.collectionName].push({ ... [relation.formName] });\",\"    }\",\"    [relation.dialogVisibleName].value = false;\",\"  }\",\"});\"]},{\"name\":\"getSelectLabel\",\"params\":\"val, options, field\",\"body\":[\"if (val === undefined || val === null || val === '') return '-'\",\"if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val\",\"const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)\",\"const pick = (item, f) => item[f]\",\"const resolve = (v) => { v = String(v).trim(); if (!v) return ''; const item = options.find(o => String(o.value) === v); if (!item) return v; const text = fields.map(f => pick(item, f)).filter(x => x !== undefined && x !== null && x !== '').join(' / '); return text || item.label || v }\",\"if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')\",\"if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')\",\"return resolve(val)\"]}],\"refs\":[{\"name\":\"[relation.formRefName]\"},{\"name\":\"[relation.editingIndexName]\",\"initialValue\":\"-1\"},{\"name\":\"[relation.dialogVisibleName]\",\"initialValue\":\"false\"}],\"reactives\":[{\"name\":\"[relation.formName]\",\"fields\":{}}]}";

    private static (string Type, string Section, string Html, string Script, int Sort)[] GetControlTemplates(
        string empty, string dict, string selectTable, string editor, string file, string image)
    {
        return new[]
        {
            ("input",    "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" :placeholder=\"$t('[field.labelKey]')\" clearable /></el-form-item></el-col>", empty, 1),
            ("input",    "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" :placeholder=\"$t('[field.labelKey]')\" clearable /></el-form-item></el-col>", empty, 2),
            ("number",   "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" :placeholder=\"$t('[field.labelKey]')\" style=\"width:100%\" /></el-form-item></el-col>", empty, 3),
            ("number",   "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input-number v-model=\"[field.formPrefix].[field.nameLower]\" :placeholder=\"$t('[field.labelKey]')\" style=\"width:100%\" /></el-form-item></el-col>", empty, 4),
            ("textarea", "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" type=\"textarea\" :rows=\"4\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 5),
            ("textarea", "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" type=\"textarea\" :rows=\"4\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 6),
            ("switch",   "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-switch v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item></el-col>", empty, 7),
            ("switch",   "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-switch v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item></el-col>", empty, 8),
            ("select",   "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"d in [field.nameLower]Options\" :key=\"d.value\" :label=\"d.label\" :value=\"d.value\" /></el-select></el-form-item></el-col>", dict, 9),
            ("select",   "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"d in [field.nameLower]Options\" :key=\"d.value\" :label=\"d.label\" :value=\"d.value\" /></el-select></el-form-item></el-col>", dict, 10),
            ("date",     "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]\" type=\"date\" style=\"width:100%\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 11),
            ("date",     "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]\" type=\"date\" style=\"width:100%\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 12),
            ("datetime", "add",    "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]\" type=\"datetime\" style=\"width:100%\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 13),
            ("datetime", "edit",   "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]\" type=\"datetime\" style=\"width:100%\" :placeholder=\"$t('[field.labelKey]')\" /></el-form-item></el-col>", empty, 14),
            // search controls
            ("search-input", "search", "<el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-input v-model=\"[field.formPrefix].[field.nameLower]\" :placeholder=\"$t('[field.labelKey]')\" clearable /></el-form-item>", empty, 15),
            ("select",       "search", "<el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower]\" style=\"width:180px\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"d in [field.nameLower]Options\" :key=\"d.value\" :label=\"d.label\" :value=\"d.value\" /></el-select></el-form-item>", dict, 16),
            ("date",         "search", "<el-form-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]Start\" type=\"date\" value-format=\"YYYY-MM-DD\" :placeholder=\"$t('start_date')\" clearable /><span style=\"margin:0 8px\">-</span><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]End\" type=\"date\" value-format=\"YYYY-MM-DD\" :placeholder=\"$t('end_date')\" clearable /></el-form-item>", empty, 17),
            ("datetime",     "search", "<el-form-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]Start\" type=\"date\" value-format=\"YYYY-MM-DD\" :placeholder=\"$t('start_date')\" clearable /><span style=\"margin:0 8px\">-</span><el-date-picker v-model=\"[field.formPrefix].[field.nameLower]End\" type=\"date\" value-format=\"YYYY-MM-DD\" :placeholder=\"$t('end_date')\" clearable /></el-form-item>", empty, 18),
            // list + detail
            ("table-column", "list",   "<el-table-column [field.prop] :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_col_[field.id]\" />", empty, 19),
            ("image",        "list",   "<el-table-column [field.prop] :label=\"$t('[field.labelKey]')\" min-width=\"180\" data-gen-id=\"gen_col_[field.id]\"><template #default=\"scope\"><div v-if=\"getUploadValues(scope.row.[field.nameLower]).length\" class=\"cm-file-link-list\"><el-link v-for=\"(u,i) in getUploadValues(scope.row.[field.nameLower])\" :key=\"i\" :href=\"u\" :download=\"getUploadFileName(u, i)\" target=\"_blank\" type=\"primary\" class=\"mr-1\">{{ getUploadFileName(u, i) }}</el-link></div><span v-else>-</span></template></el-table-column>", empty, 20),
            ("file",         "list",   "<el-table-column [field.prop] :label=\"$t('[field.labelKey]')\" min-width=\"180\" data-gen-id=\"gen_col_[field.id]\"><template #default=\"scope\"><div v-if=\"getUploadValues(scope.row.[field.nameLower]).length\" class=\"cm-file-link-list\"><el-link v-for=\"(u,i) in getUploadValues(scope.row.[field.nameLower])\" :key=\"i\" :href=\"u\" :download=\"getUploadFileName(u, i)\" target=\"_blank\" type=\"primary\" class=\"mr-1\">{{ getUploadFileName(u, i) }}</el-link></div><span v-else>-</span></template></el-table-column>", empty, 20),
            ("select-table", "list",   "<el-table-column [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_col_[field.id]_[displayField.nameLower]\"><template #default=\"scope\">[displayField.listContent]</template></el-table-column>", selectTable, 20),
            ("editor",       "list",   "<el-table-column [field.prop] :label=\"$t('[field.labelKey]')\" min-width=\"240\" data-gen-id=\"gen_col_[field.id]\"><template #default=\"scope\"><div class=\"rich-text-cell\" style=\"max-height:72px;overflow:hidden;line-height:1.5\" v-html=\"scope.row.[field.nameLower] || '-'\"></div></template></el-table-column>", empty, 20),
            ("input",        "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\">{{ detail.[field.nameLower] }}</el-descriptions-item>", empty, 21),
            ("editor",       "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" :span=\"2\" data-gen-id=\"gen_field_[field.id]\"><div class=\"rich-text-detail\" style=\"max-width:100%;overflow:auto;line-height:1.6\" v-html=\"detail.[field.nameLower] || '-'\"></div></el-descriptions-item>", empty, 21),
            // advanced controls (add/edit only)
            ("select-table", "add",  "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" prop=\"[field.nameLower][field.multipleSuffix]\" data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.relatedEntityNameLower]Options\" :key=\"item.value\" :label=\"[field.relatedDisplayLabelExpr]\" :value=\"item.value\"><span [v-for=\"displayField in field.displayFields\"] style=\"margin-right:8px\">{{ [displayField.itemValueExpr] }}</span></el-option></el-select></el-form-item></el-col>", selectTable, 21),
            ("select-table", "edit", "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" prop=\"[field.nameLower][field.multipleSuffix]\" data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.relatedEntityNameLower]Options\" :key=\"item.value\" :label=\"[field.relatedDisplayLabelExpr]\" :value=\"item.value\"><span [v-for=\"displayField in field.displayFields\"] style=\"margin-right:8px\">{{ [displayField.itemValueExpr] }}</span></el-option></el-select></el-form-item></el-col>", selectTable, 22),
            ("editor",      "add",  "<el-col :xs=\"24\" :sm=\"24\"><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] class=\"editor-form-item\" data-gen-id=\"gen_field_[field.id]\"><div class=\"editor-wrap\" style=\"width:100%;border:1px solid var(--el-border-color);border-radius:4px;overflow:hidden\"><div :id=\"'[field.nameLower]Toolbar'\" class=\"editor-toolbar\" style=\"border-bottom:1px solid var(--el-border-color)\"></div><div :id=\"'[field.nameLower]Editor'\" class=\"editor-content\" style=\"min-height:220px\"></div></div></el-form-item></el-col>", editor, 23),
            ("editor",      "edit", "<el-col :xs=\"24\" :sm=\"24\"><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] class=\"editor-form-item\" data-gen-id=\"gen_field_[field.id]\"><div class=\"editor-wrap\" style=\"width:100%;border:1px solid var(--el-border-color);border-radius:4px;overflow:hidden\"><div :id=\"'[field.nameLower]Toolbar'\" class=\"editor-toolbar\" style=\"border-bottom:1px solid var(--el-border-color)\"></div><div :id=\"'[field.nameLower]Editor'\" class=\"editor-content\" style=\"min-height:220px\"></div></div></el-form-item></el-col>", editor, 24),
            ("file",        "add",  "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-upload class=\"cm-file-upload\" :http-request=\"uploadFile\" :before-upload=\"beforeFileUpload\" :on-success=\"(url) => [field.formPrefix].[field.nameLower] = url\" :on-remove=\"() => [field.formPrefix].[field.nameLower] = ''\" :file-list=\"toUploadFileList([field.formPrefix].[field.nameLower])\" :limit=\"1\"><el-button type=\"primary\">{{ $t('upload_file') }}</el-button></el-upload></el-form-item></el-col>", file, 25),
            ("file",        "edit", "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-upload class=\"cm-file-upload\" :http-request=\"uploadFile\" :before-upload=\"beforeFileUpload\" :on-success=\"(url) => [field.formPrefix].[field.nameLower] = url\" :on-remove=\"() => [field.formPrefix].[field.nameLower] = ''\" :file-list=\"toUploadFileList([field.formPrefix].[field.nameLower])\" :limit=\"1\"><el-button type=\"primary\">{{ $t('upload_file') }}</el-button></el-upload></el-form-item></el-col>", file, 26),
            ("image",       "add",  "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-upload :http-request=\"uploadFile\" :before-upload=\"beforeImageUpload\" :on-success=\"(url) => [field.formPrefix].[field.nameLower] = url\" :file-list=\"[field.formPrefix].[field.nameLower] ? [{ url: [field.formPrefix].[field.nameLower] }] : []\" list-type=\"picture-card\" :limit=\"1\"><el-icon><Plus /></el-icon></el-upload></el-form-item></el-col>", image, 27),
            ("image",       "edit", "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-upload :http-request=\"uploadFile\" :before-upload=\"beforeImageUpload\" :on-success=\"(url) => [field.formPrefix].[field.nameLower] = url\" :file-list=\"[field.formPrefix].[field.nameLower] ? [{ url: [field.formPrefix].[field.nameLower] }] : []\" list-type=\"picture-card\" :limit=\"1\"><el-icon><Plus /></el-icon></el-upload></el-form-item></el-col>", image, 28),
            // cascader
            ("cascader",    "add",  "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-cascader v-model=\"[field.formPrefix].[field.nameLower]\" :options=\"[field.relatedEntityNameLower]Tree\" :props=\"{ value: 'id', label: '[field.relatedDisplayLabel]', checkStrictly: true, emitPath: false }\" style=\"width:100%\" clearable /></el-form-item></el-col>", empty, 29),
            ("cascader",    "edit", "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-cascader v-model=\"[field.formPrefix].[field.nameLower]\" :options=\"[field.relatedEntityNameLower]Tree\" :props=\"{ value: 'id', label: '[field.relatedDisplayLabel]', checkStrictly: true, emitPath: false }\" style=\"width:100%\" clearable /></el-form-item></el-col>", empty, 30),
            // search — 补全
            ("switch",       "search", "<el-form-item :label=\"$t('[field.labelKey]')\"><el-switch v-model=\"[field.formPrefix].[field.nameLower]\" /></el-form-item>", empty, 31),
            ("cascader",     "search", "<el-form-item :label=\"$t('[field.labelKey]')\"><el-cascader v-model=\"[field.formPrefix].[field.nameLower]\" :options=\"[field.relatedEntityNameLower]Tree\" :props=\"{ value: 'id', label: '[field.relatedDisplayLabel]', checkStrictly: true, emitPath: false }\" clearable /></el-form-item>", empty, 32),
            ("select-enum",  "search", "<el-form-item :label=\"$t('[field.labelKey]')\"><el-select v-model=\"[field.formPrefix].[field.nameLower]\" style=\"width:180px\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.enumTypeName]Options\" :key=\"item.value\" :label=\"item.label\" :value=\"item.value\" /></el-select></el-form-item>", empty, 33),
            ("select-table", "search", "<el-form-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower]\" style=\"width:180px\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.relatedEntityNameLower]Options\" :key=\"item.value\" :label=\"[field.relatedDisplayLabelExpr]\" :value=\"item.value\"><span [v-for=\"displayField in field.displayFields\"] style=\"margin-right:8px\">{{ [displayField.itemValueExpr] }}</span></el-option></el-select></el-form-item>", selectTable, 34),
            // detail — 补全
            ("select",       "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\">{{ getDictLabel(detail.[field.nameLower], [field.nameLower]Options) }}</el-descriptions-item>", dict, 35),
            ("select-table", "detail", "<el-descriptions-item [v-for=\"displayField in field.displayFields\"] :label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_field_[field.id]_[displayField.nameLower]\">[displayField.detailContent]</el-descriptions-item>", selectTable, 35),
            ("image",        "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><div v-if=\"getUploadValues(detail.[field.nameLower]).length\" class=\"cm-file-link-list\"><el-link v-for=\"(u,i) in getUploadValues(detail.[field.nameLower])\" :key=\"i\" :href=\"u\" :download=\"getUploadFileName(u, i)\" target=\"_blank\" type=\"primary\" class=\"mr-1\">{{ getUploadFileName(u, i) }}</el-link></div><span v-else>-</span></el-descriptions-item>", empty, 36),
            ("file",         "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><div v-if=\"getUploadValues(detail.[field.nameLower]).length\" class=\"cm-file-link-list\"><el-link v-for=\"(u,i) in getUploadValues(detail.[field.nameLower])\" :key=\"i\" :href=\"u\" :download=\"getUploadFileName(u, i)\" target=\"_blank\" type=\"primary\" class=\"mr-1\">{{ getUploadFileName(u, i) }}</el-link></div><span v-else>-</span></el-descriptions-item>", empty, 37),
            ("cascader",     "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\">{{ detail.[field.nameLower] }}</el-descriptions-item>", empty, 38),
            ("date",         "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\">{{ formatDate(detail.[field.nameLower]) }}</el-descriptions-item>", empty, 39),
            ("datetime",     "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\">{{ formatDate(detail.[field.nameLower], true) }}</el-descriptions-item>", empty, 40),
            ("switch",       "detail", "<el-descriptions-item :label=\"$t('[field.labelKey]')\" data-gen-id=\"gen_field_[field.id]\"><el-tag :type=\"detail.[field.nameLower] ? 'success' : 'danger'\">{{ detail.[field.nameLower] ? $t('yes') : $t('no') }}</el-tag></el-descriptions-item>", empty, 41),
            // select-enum add/edit
            ("select-enum",  "add",  "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.enumTypeName]Options\" :key=\"item.value\" :label=\"item.label\" :value=\"item.value\" /></el-select></el-form-item></el-col>", empty, 42),
            ("select-enum",  "edit", "<el-col [field.col]><el-form-item :label=\"$t('[field.labelKey]')\" [field.prop] data-gen-id=\"gen_field_[field.id]\"><el-select v-model=\"[field.formPrefix].[field.nameLower][field.multipleSuffix]\" style=\"width:100%\" clearable [field.multipleAttr] :placeholder=\"$t('[field.labelKey]')\"><el-option v-for=\"item in [field.enumTypeName]Options\" :key=\"item.value\" :label=\"item.label\" :value=\"item.value\" /></el-select></el-form-item></el-col>", empty, 43),
        };
    }

    private static string GetEditorScript() => JsonSerializer.Serialize(new
    {
        imports = new object[] {
            new { path = "vue", destructured = "ref, watch, onMounted, onUnmounted" },
            new { path = "@wangeditor/editor", destructured = "createEditor, createToolbar" },
            new { path = "@wangeditor/editor/dist/css/style.css" }
        },
        uses = new object[0],
        refs = new object[] {
            new { name = "[field.nameLower]EditorInstance", initialValue = "null" }
        },
        reactives = new object[0],
        functions = new object[] {
            new Dictionary<string, object> {
                ["name"] = "init[field.name]Editor",
                ["async"] = true,
                ["body"] = new[] {
                    "const { createEditor, createToolbar } = await import('@wangeditor/editor');",
                    "await import('@wangeditor/editor/dist/css/style.css');",
                    "const editor = createEditor({",
                    "  selector: '#[field.nameLower]Editor',",
                    "  html: [field.formPrefix].[field.nameLower] || '',",
                    "  config: {",
                    "    placeholder: '[field.description]',",
                    "    onChange(editor) { [field.formPrefix].[field.nameLower] = editor.getHtml(); }",
                    "  }",
                    "});",
                    "createToolbar({ editor, selector: '#[field.nameLower]Toolbar', config: {} });",
                    "[field.nameLower]EditorInstance.value = editor;"
                }
            }
        },
        hooks = new object[] {
            new { name = "onMounted", body = new[] { "init[field.name]Editor();" } },
            new { name = "onUnmounted", body = new[] { "if ([field.nameLower]EditorInstance.value) { [field.nameLower]EditorInstance.value.destroy(); }" } }
        },
        computed = new object[0],
        watches = new object[] {
            new {
                source = "[field.formPrefix].[field.nameLower]",
                body = new[] {
                    "const editor = [field.nameLower]EditorInstance.value;",
                    "const html = [field.formPrefix].[field.nameLower] || '';",
                    "if (editor && editor.getHtml() !== html) { editor.setHtml(html); }"
                }
            }
        }
    });

    private static string GetSelectDictScript() => JsonSerializer.Serialize(new
    {
        imports = new object[] {
            new { path = "@/api/system/dict", destructured = "getDataListByType" }
        },
        uses = new object[0],
        refs = new object[] {
            new { name = "[field.nameLower]Options", initialValue = "[]" }
        },
        reactives = new object[0],
        functions = new object[0],
        hooks = new object[] {
            new { name = "onMounted", body = new[] { "getDataListByType('[field.selectOptions]').then(res => { [field.nameLower]Options.value = res.map(item => ({ label: item.label, value: item.value })) })" } }
        },
        computed = new object[0],
        watches = new object[0]
    });

    private static string GetSelectTableScript() => JsonSerializer.Serialize(new
    {
        imports = new object[] {
            new { path = "@/api/[field.relatedModuleNameLower]/[field.relatedEntityNameLower]", destructured = "getList as get[field.relatedEntityName]List" }
        },
        uses = new object[0],
        refs = new object[] {
            new { name = "[field.relatedEntityNameLower]Options", initialValue = "[]" }
        },
        reactives = new object[0],
        functions = new object[0],
        hooks = new object[] {
            new { name = "onMounted", body = new[] { "get[field.relatedEntityName]List().then(res => { [field.relatedEntityNameLower]Options.value = res.map(item => ({ value: item.[field.relatedEntityIdFieldLower], ...item })) })" } }
        },
        computed = new object[0],
        watches = new object[0]
    });

    private static string GetFileScript() => JsonSerializer.Serialize(new
    {
        imports = new object[] {
            new { path = "element-plus", destructured = "ElMessage" },
            new { path = "@/api/system/file", destructured = "upload" }
        },
        uses = new object[0],
        refs = new object[0],
        reactives = new object[0],
        functions = new object[] {
            new Dictionary<string, object> {
                ["name"] = "uploadFile",
                ["params"] = "opt",
                ["async"] = true,
                ["body"] = new[] {
                    "try {",
                    "  const res = await upload(opt.file);",
                    "  const url = res?.accessUrl || res?.AccessUrl || res?.fileUrl || res?.FileUrl || res;",
                    "  opt.onSuccess(url);",
                    "} catch (e) {",
                    "  opt.onError(e);",
                    "}"
                }
            },
            new Dictionary<string, object> {
                ["name"] = "toUploadFileList",
                ["params"] = "value",
                ["body"] = new[] {
                    "if (!value) return [];",
                    "return String(value).split(',').filter(Boolean).map((url, index) => {",
                    "  const rawName = String(url).split('/').pop() || `附件${index + 1}`;",
                    "  let name = rawName;",
                    "  try { name = decodeURIComponent(rawName); } catch { name = rawName; }",
                    "  return { name, url };",
                    "});"
                }
            },
            new Dictionary<string, object> {
                ["name"] = "beforeFileUpload",
                ["params"] = "file",
                ["body"] = new[] {
                    "const isLt10M = file.size / 1024 / 1024 < 10;",
                    "if (!isLt10M) { ElMessage.error('File size cannot exceed 10MB'); return false; }",
                    "return true;"
                }
            },
        },
        hooks = new object[0],
        computed = new object[0],
        watches = new object[0]
    });

    private static string GetImageScript() => JsonSerializer.Serialize(new
    {
        imports = new object[] {
            new { path = "element-plus", destructured = "ElMessage" },
            new { path = "@element-plus/icons-vue", destructured = "Plus" },
            new { path = "@/api/system/file", destructured = "upload" }
        },
        uses = new object[0],
        refs = new object[0],
        reactives = new object[0],
        functions = new object[] {
            new Dictionary<string, object> {
                ["name"] = "uploadFile",
                ["params"] = "opt",
                ["async"] = true,
                ["body"] = new[] {
                    "try {",
                    "  const res = await upload(opt.file);",
                    "  const url = res?.accessUrl || res?.AccessUrl || res?.fileUrl || res?.FileUrl || res;",
                    "  opt.onSuccess(url);",
                    "} catch (e) {",
                    "  opt.onError(e);",
                    "}"
                }
            },
            new Dictionary<string, object> {
                ["name"] = "beforeImageUpload",
                ["params"] = "file",
                ["body"] = new[] {
                    "const isImage = file.type.startsWith('image/');",
                    "if (!isImage) { ElMessage.error('Only image files are allowed'); return false; }",
                    "const isLt2M = file.size / 1024 / 1024 < 2;",
                    "if (!isLt2M) { ElMessage.error('File size cannot exceed 2MB'); return false; }",
                    "return true;"
                }
            },
        },
        hooks = new object[0],
        computed = new object[0],
        watches = new object[0]
    });
}
