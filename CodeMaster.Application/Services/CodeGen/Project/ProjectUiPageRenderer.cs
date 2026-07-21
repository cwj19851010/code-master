using System.Net;
using System.Text;
using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

public static class ProjectUiPageRenderer
{
    private static readonly HashSet<string> SupportedPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Login",
        "Dashboard"
    };

    public static async Task<ProjectUiEnhancementResultDto> ApplyAsync(
        Project project,
        IReadOnlyCollection<ProjectModule> modules,
        IReadOnlyCollection<ModuleEntity> entities,
        ProjectUiEnhancementDto input,
        string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(input);

        var page = NormalizePage(input.Page);
        var frontendRoot = Path.Combine(projectRoot, $"{project.ProjectName}.Vue");
        if (!Directory.Exists(frontendRoot))
            throw new DirectoryNotFoundException($"Frontend project directory not found: {frontendRoot}");

        var relativePath = page == "Login"
            ? Path.Combine("src", "views", "login", "index.vue")
            : Path.Combine("src", "views", "dashboard", "index.vue");
        var targetPath = Path.GetFullPath(Path.Combine(frontendRoot, relativePath));
        EnsureInsideDirectory(frontendRoot, targetPath);

        if (!File.Exists(targetPath))
            throw new FileNotFoundException($"UI page not found: {targetPath}", targetPath);

        var normalizedInput = NormalizeInput(project, modules, entities, input, page);
        var content = page == "Login"
            ? RenderLogin(project, modules, entities, normalizedInput)
            : RenderDashboard(project, modules, entities, normalizedInput);

        var backupRoot = Path.Combine(projectRoot, ".codemaster", "backups", "ui", DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        Directory.CreateDirectory(backupRoot);
        var backupPath = Path.Combine(backupRoot, $"{page.ToLowerInvariant()}.index.vue");
        File.Copy(targetPath, backupPath, overwrite: true);

        var temporaryPath = targetPath + ".codemaster.tmp";
        await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporaryPath, targetPath, overwrite: true);

        return new ProjectUiEnhancementResultDto
        {
            Success = true,
            Message = $"{page} page updated",
            Page = page,
            FilePath = targetPath,
            BackupPath = backupPath
        };
    }

    private static ProjectUiEnhancementDto NormalizeInput(
        Project project,
        IReadOnlyCollection<ProjectModule> modules,
        IReadOnlyCollection<ModuleEntity> entities,
        ProjectUiEnhancementDto input,
        string page)
    {
        var style = NormalizeStyle(input.Style);
        var palette = GetPalette(style);
        var headline = NormalizeText(input.Headline, 80) ?? project.DisplayName;
        var subtitle = NormalizeText(input.Subtitle, 240)
            ?? NormalizeText(project.Description, 240)
            ?? (page == "Login" ? "统一、安全、高效的业务管理入口" : "聚焦核心业务数据与日常工作进度");
        var highlights = input.Highlights
            .Select(item => NormalizeText(item, 80))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        if (highlights.Count == 0)
        {
            highlights.Add($"{modules.Count} 个业务模块");
            highlights.Add($"{entities.Count} 个核心实体");
            highlights.Add($"{project.DatabaseType} 数据存储");
        }

        return new ProjectUiEnhancementDto
        {
            ProjectId = project.Id,
            Page = page,
            Style = style,
            Headline = headline,
            Subtitle = subtitle,
            Highlights = highlights,
            PrimaryColor = NormalizeColor(input.PrimaryColor) ?? palette.Primary,
            SecondaryColor = NormalizeColor(input.SecondaryColor) ?? palette.Secondary
        };
    }

    private static string RenderLogin(
        Project project,
        IReadOnlyCollection<ProjectModule> modules,
        IReadOnlyCollection<ModuleEntity> entities,
        ProjectUiEnhancementDto input)
    {
        var projectName = Html(project.DisplayName);
        var headline = Html(input.Headline);
        var subtitle = Html(input.Subtitle);
        var highlights = input.Highlights.Take(3).ToList();
        while (highlights.Count < 3)
            highlights.Add("稳定可靠的业务能力");

        var primary = input.PrimaryColor!;
        var secondary = input.SecondaryColor!;
        var moduleCount = modules.Count;
        var entityCount = entities.Count;
        var databaseType = Html(project.DatabaseType.ToString());

        return $$$"""
        <template>
          <main class="login-page">
            <theme-picker class="theme-control" />

            <section class="product-panel">
              <div class="product-kicker">{{{Html(project.ProjectName)}}} / BUSINESS CONSOLE</div>
              <div class="product-heading">
                <span class="product-mark"><el-icon><DataBoard /></el-icon></span>
                <div>
                  <p>{{{projectName}}}</p>
                  <h1>{{{headline}}}</h1>
                </div>
              </div>
              <p class="product-copy">{{{subtitle}}}</p>

              <div class="feature-list">
                <div>
                  <span>01</span>
                  <strong>{{{Html(highlights[0])}}}</strong>
                </div>
                <div>
                  <span>02</span>
                  <strong>{{{Html(highlights[1])}}}</strong>
                </div>
                <div>
                  <span>03</span>
                  <strong>{{{Html(highlights[2])}}}</strong>
                </div>
              </div>

              <div class="project-facts">
                <div><strong>{{{moduleCount}}}</strong><span>业务模块</span></div>
                <div><strong>{{{entityCount}}}</strong><span>核心实体</span></div>
                <div><strong>{{{databaseType}}}</strong><span>数据引擎</span></div>
              </div>
            </section>

            <section class="access-panel">
              <div class="access-content">
                <div class="access-header">
                  <span>SECURE ACCESS</span>
                  <h2>{{ t('login_title') }}</h2>
                  <p>登录后进入 {{{projectName}}} 工作台</p>
                </div>

                <el-form ref="loginFormRef" :model="loginForm" :rules="loginRules" class="login-form">
                  <el-form-item prop="username">
                    <el-input
                      v-model="loginForm.username"
                      :placeholder="$t2('please_input', 'username')"
                      size="large"
                      :prefix-icon="User"
                      clearable
                    />
                  </el-form-item>

                  <el-form-item prop="password">
                    <el-input
                      v-model="loginForm.password"
                      :type="passwordType"
                      :placeholder="$t2('please_input', 'password')"
                      size="large"
                      :prefix-icon="Lock"
                      @keyup.enter="handleLogin"
                    >
                      <template #suffix>
                        <el-icon class="password-toggle" @click="showPwd">
                          <View v-if="passwordType === 'password'" />
                          <Hide v-else />
                        </el-icon>
                      </template>
                    </el-input>
                  </el-form-item>

                  <el-button
                    :loading="loading"
                    type="primary"
                    size="large"
                    class="login-button"
                    @click.prevent="handleLogin"
                  >
                    <span v-if="!loading">{{ t('login') }}</span>
                    <span v-else>{{ t('login_loading') }}</span>
                    <el-icon v-if="!loading"><Right /></el-icon>
                  </el-button>
                </el-form>

                <p class="access-footer">{{ t('copyright') }}</p>
              </div>
            </section>
          </main>
        </template>

        <script setup>
        import { reactive, ref } from 'vue'
        import { useRouter } from 'vue-router'
        import { ElMessage } from 'element-plus'
        import { DataBoard, Hide, Lock, Right, User, View } from '@element-plus/icons-vue'
        import { useI18n } from 'vue-i18n'
        import { useUserStore } from '@/stores/user'
        import { t2 } from '@/i18n'
        import ThemePicker from '@/layout/components/ThemePicker.vue'

        const router = useRouter()
        const userStore = useUserStore()
        const loginFormRef = ref(null)
        const { t } = useI18n()

        const loginForm = reactive({
          username: 'admin',
          password: 'admin123'
        })

        const loginRules = {
          username: [{ required: true, trigger: 'blur', message: t2('please_input', 'username') }],
          password: [{ required: true, trigger: 'blur', message: t2('please_input', 'password') }]
        }

        const loading = ref(false)
        const passwordType = ref('password')

        const showPwd = () => {
          passwordType.value = passwordType.value === 'password' ? 'text' : 'password'
        }

        const handleLogin = () => {
          loginFormRef.value?.validate(async valid => {
            if (!valid) return

            loading.value = true
            try {
              await userStore.login({
                userName: loginForm.username,
                password: loginForm.password
              })
              ElMessage.success(t('login_success'))
              router.push('/')
            } catch (error) {
              ElMessage.error(error?.message || t('login_failed'))
            } finally {
              loading.value = false
            }
          })
        }
        </script>

        <style scoped lang="scss">
        .login-page {
          --login-primary: {{{primary}}};
          --login-secondary: {{{secondary}}};
          display: grid;
          grid-template-columns: minmax(0, 1.2fr) minmax(420px, 0.8fr);
          width: 100%;
          min-height: 100vh;
          color: #e5e7eb;
          background: #091018;
        }

        .theme-control {
          position: fixed;
          top: 20px;
          right: 22px;
          z-index: 3;
          padding: 5px;
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 8px;
          background: rgba(15, 23, 42, 0.82);
          backdrop-filter: blur(12px);
        }

        .product-panel {
          position: relative;
          display: flex;
          flex-direction: column;
          justify-content: center;
          min-height: 100vh;
          padding: clamp(48px, 7vw, 112px);
          overflow: hidden;
          background:
            linear-gradient(110deg, color-mix(in srgb, var(--login-primary) 30%, transparent), transparent 42%),
            linear-gradient(155deg, #0b1220 0%, #111827 58%, #101820 100%);
        }

        .product-panel::before {
          content: '';
          position: absolute;
          inset: 0;
          opacity: 0.26;
          background-image:
            linear-gradient(rgba(148, 163, 184, 0.16) 1px, transparent 1px),
            linear-gradient(90deg, rgba(148, 163, 184, 0.16) 1px, transparent 1px);
          background-size: 48px 48px;
          mask-image: linear-gradient(90deg, #000 0%, transparent 90%);
          pointer-events: none;
        }

        .product-panel > * {
          position: relative;
        }

        .product-kicker {
          margin-bottom: 34px;
          color: color-mix(in srgb, var(--login-secondary) 85%, #fff);
          font-size: 12px;
          font-weight: 800;
          text-transform: uppercase;
        }

        .product-heading {
          display: flex;
          align-items: center;
          gap: 22px;
        }

        .product-heading p,
        .product-heading h1,
        .product-copy {
          margin: 0;
        }

        .product-heading p {
          margin-bottom: 8px;
          color: rgba(226, 232, 240, 0.68);
          font-size: 15px;
        }

        .product-heading h1 {
          max-width: 760px;
          color: #fff;
          font-size: clamp(38px, 5vw, 70px);
          font-weight: 800;
          line-height: 1.06;
        }

        .product-mark {
          display: inline-flex;
          width: 74px;
          height: 74px;
          flex: 0 0 auto;
          align-items: center;
          justify-content: center;
          border: 1px solid rgba(255, 255, 255, 0.18);
          border-radius: 8px;
          color: #fff;
          background: linear-gradient(135deg, var(--login-primary), var(--login-secondary));
          font-size: 36px;
          box-shadow: 0 24px 60px color-mix(in srgb, var(--login-primary) 30%, transparent);
        }

        .product-copy {
          max-width: 720px;
          margin-top: 28px;
          color: rgba(226, 232, 240, 0.76);
          font-size: 17px;
          line-height: 1.8;
        }

        .feature-list {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 1px;
          max-width: 820px;
          margin-top: 56px;
          border: 1px solid rgba(148, 163, 184, 0.2);
          background: rgba(148, 163, 184, 0.2);
        }

        .feature-list div {
          min-height: 108px;
          padding: 20px;
          background: rgba(8, 15, 26, 0.88);
        }

        .feature-list span,
        .feature-list strong {
          display: block;
        }

        .feature-list span {
          color: var(--login-secondary);
          font-size: 12px;
          font-weight: 800;
        }

        .feature-list strong {
          margin-top: 20px;
          color: #f8fafc;
          font-size: 15px;
          line-height: 1.45;
        }

        .project-facts {
          display: flex;
          gap: 34px;
          margin-top: 34px;
        }

        .project-facts strong,
        .project-facts span {
          display: block;
        }

        .project-facts strong {
          color: #fff;
          font-size: 18px;
        }

        .project-facts span {
          margin-top: 4px;
          color: rgba(203, 213, 225, 0.58);
          font-size: 12px;
        }

        .access-panel {
          display: flex;
          align-items: center;
          justify-content: center;
          min-height: 100vh;
          padding: 72px 54px;
          color: #111827;
          background: #f8fafc;
        }

        .access-content {
          width: min(390px, 100%);
        }

        .access-header span {
          color: var(--login-primary);
          font-size: 12px;
          font-weight: 800;
        }

        .access-header h2 {
          margin: 14px 0 8px;
          color: #111827;
          font-size: 32px;
          font-weight: 800;
        }

        .access-header p {
          margin: 0;
          color: #64748b;
          line-height: 1.6;
        }

        .login-form {
          margin-top: 36px;
        }

        .login-form :deep(.el-form-item) {
          margin-bottom: 20px;
        }

        .login-form :deep(.el-input__wrapper) {
          min-height: 48px;
          border-radius: 8px;
          box-shadow: 0 0 0 1px #cbd5e1 inset;
        }

        .login-form :deep(.el-input__wrapper.is-focus) {
          box-shadow: 0 0 0 1px var(--login-primary) inset, 0 0 0 4px color-mix(in srgb, var(--login-primary) 12%, transparent);
        }

        .password-toggle {
          cursor: pointer;
        }

        .login-button {
          display: flex;
          width: 100%;
          height: 48px;
          align-items: center;
          justify-content: center;
          gap: 8px;
          margin-top: 8px;
          border: 0;
          border-radius: 8px;
          background: linear-gradient(110deg, var(--login-primary), color-mix(in srgb, var(--login-primary) 72%, #111827), var(--login-secondary));
          font-size: 16px;
          font-weight: 700;
        }

        .access-footer {
          margin: 34px 0 0;
          color: #94a3b8;
          font-size: 12px;
          text-align: center;
        }

        @media (max-width: 980px) {
          .login-page {
            grid-template-columns: 1fr;
          }

          .product-panel,
          .access-panel {
            min-height: auto;
          }

          .product-panel {
            padding: 68px 32px 42px;
          }

          .access-panel {
            padding: 48px 24px 64px;
          }
        }

        @media (max-width: 640px) {
          .product-heading {
            align-items: flex-start;
            flex-direction: column;
          }

          .product-heading h1 {
            font-size: 38px;
          }

          .feature-list {
            grid-template-columns: 1fr;
          }

          .project-facts {
            flex-wrap: wrap;
          }
        }
        </style>
        """;
    }

    private static string RenderDashboard(
        Project project,
        IReadOnlyCollection<ProjectModule> modules,
        IReadOnlyCollection<ModuleEntity> entities,
        ProjectUiEnhancementDto input)
    {
        var orderedModules = modules.OrderBy(item => item.OrderNum).ThenBy(item => item.ModuleName).ToList();
        var orderedEntities = entities.OrderBy(item => item.OrderNum).ThenBy(item => item.Name).ToList();
        var moduleItems = orderedModules.Select(module => new
        {
            name = module.ModuleDescription,
            code = module.ModuleName,
            description = NormalizeText(module.Remark, 100) ?? $"{module.ModuleDescription}业务能力",
            entityCount = orderedEntities.Count(entity => entity.ModuleId == module.Id)
        }).ToList();
        var entityItems = orderedEntities.Take(8).Select(entity => new
        {
            name = entity.Description,
            code = entity.Name,
            module = orderedModules.FirstOrDefault(module => module.Id == entity.ModuleId)?.ModuleDescription ?? "未分组"
        }).ToList();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var modulesJson = JsonSerializer.Serialize(moduleItems, jsonOptions);
        var entitiesJson = JsonSerializer.Serialize(entityItems, jsonOptions);
        var highlightsJson = JsonSerializer.Serialize(input.Highlights.Take(6), jsonOptions);
        var primary = input.PrimaryColor!;
        var secondary = input.SecondaryColor!;

        return $$$"""
        <template>
          <main class="dashboard-page">
            <header class="dashboard-header">
              <div>
                <p class="eyebrow">{{{Html(project.ProjectName)}}} / OVERVIEW</p>
                <h1>{{{Html(input.Headline)}}}</h1>
                <p class="header-copy">{{{Html(input.Subtitle)}}}</p>
              </div>
              <div class="environment-badge">
                <el-icon><CircleCheck /></el-icon>
                <div>
                  <strong>系统运行就绪</strong>
                  <span>{{{Html(project.DatabaseType.ToString())}}} · Frontend {{{project.FrontendPort?.ToString() ?? "-"}}} · API {{{project.BackendPort?.ToString() ?? "-"}}}</span>
                </div>
              </div>
            </header>

            <section class="metric-grid">
              <article v-for="item in metrics" :key="item.label" class="metric-item">
                <span class="metric-icon" :class="item.tone"><el-icon><component :is="item.icon" /></el-icon></span>
                <div>
                  <p>{{ item.label }}</p>
                  <strong>{{ item.value }}</strong>
                  <span>{{ item.description }}</span>
                </div>
              </article>
            </section>

            <section class="content-grid">
              <div class="module-section">
                <div class="section-heading">
                  <div>
                    <p>BUSINESS MAP</p>
                    <h2>业务模块</h2>
                  </div>
                  <el-tag effect="plain">{{ modules.length }} Modules</el-tag>
                </div>

                <div v-if="modules.length" class="module-list">
                  <article v-for="(module, index) in modules" :key="module.code" class="module-item">
                    <span class="module-index">{{ String(index + 1).padStart(2, '0') }}</span>
                    <div>
                      <strong>{{ module.name }}</strong>
                      <p>{{ module.description }}</p>
                    </div>
                    <div class="module-meta">
                      <span>{{ module.code }}</span>
                      <b>{{ module.entityCount }} 个实体</b>
                    </div>
                  </article>
                </div>
                <el-empty v-else description="尚未配置业务模块" />
              </div>

              <aside class="side-section">
                <div class="section-heading compact">
                  <div>
                    <p>FOCUS</p>
                    <h2>当前重点</h2>
                  </div>
                </div>
                <div class="focus-list">
                  <div v-for="(item, index) in highlights" :key="item">
                    <el-icon><Aim /></el-icon>
                    <span>{{ item }}</span>
                    <b>{{ String(index + 1).padStart(2, '0') }}</b>
                  </div>
                </div>

                <div class="section-heading compact entity-heading">
                  <div>
                    <p>DATA MODEL</p>
                    <h2>核心实体</h2>
                  </div>
                </div>
                <div class="entity-cloud">
                  <el-tag v-for="entity in entities" :key="entity.code" effect="plain">
                    {{ entity.name }} · {{ entity.module }}
                  </el-tag>
                </div>
              </aside>
            </section>
          </main>
        </template>

        <script setup>
        import { Aim, CircleCheck, Collection, DataLine, Files, Grid } from '@element-plus/icons-vue'

        const modules = {{{modulesJson}}}
        const entities = {{{entitiesJson}}}
        const highlights = {{{highlightsJson}}}

        const metrics = [
          {
            label: '业务模块',
            value: '{{{modules.Count}}}',
            description: '覆盖当前系统的业务边界',
            icon: Grid,
            tone: 'blue'
          },
          {
            label: '核心实体',
            value: '{{{entities.Count}}}',
            description: '已纳入统一数据模型',
            icon: Collection,
            tone: 'green'
          },
          {
            label: '数据引擎',
            value: '{{{Html(project.DatabaseType.ToString())}}}',
            description: '当前项目持久化方案',
            icon: DataLine,
            tone: 'amber'
          },
          {
            label: '生成状态',
            value: '{{{entities.Count(entity => entity.IsGenerated)}}} / {{{entities.Count}}}',
            description: '实体代码已生成进度',
            icon: Files,
            tone: 'slate'
          }
        ]
        </script>

        <style scoped lang="scss">
        .dashboard-page {
          --dashboard-primary: {{{primary}}};
          --dashboard-secondary: {{{secondary}}};
          display: flex;
          flex-direction: column;
          gap: 24px;
          color: var(--app-text, #1f2937);
        }

        .dashboard-header {
          display: flex;
          align-items: flex-end;
          justify-content: space-between;
          gap: 32px;
          padding: 12px 0 28px;
          border-bottom: 1px solid var(--app-border, #dbe4ef);
        }

        .eyebrow,
        .section-heading p {
          margin: 0;
          color: var(--dashboard-primary);
          font-size: 12px;
          font-weight: 800;
          text-transform: uppercase;
        }

        .dashboard-header h1 {
          margin: 8px 0 10px;
          color: var(--app-text, #111827);
          font-size: 34px;
          font-weight: 800;
        }

        .header-copy {
          max-width: 760px;
          margin: 0;
          color: var(--app-text-muted, #64748b);
          line-height: 1.7;
        }

        .environment-badge {
          display: flex;
          min-width: 290px;
          align-items: center;
          gap: 12px;
          padding: 14px 16px;
          border-left: 3px solid var(--dashboard-secondary);
          background: var(--app-surface-soft, #f8fafc);
        }

        .environment-badge .el-icon {
          color: var(--dashboard-secondary);
          font-size: 24px;
        }

        .environment-badge strong,
        .environment-badge span {
          display: block;
        }

        .environment-badge span {
          margin-top: 4px;
          color: var(--app-text-muted, #64748b);
          font-size: 12px;
        }

        .metric-grid {
          display: grid;
          grid-template-columns: repeat(4, minmax(0, 1fr));
          gap: 14px;
        }

        .metric-item {
          display: flex;
          min-height: 132px;
          gap: 14px;
          padding: 20px;
          border: 1px solid var(--app-border, #dbe4ef);
          border-radius: 8px;
          background: var(--app-surface, #fff);
        }

        .metric-icon {
          display: inline-flex;
          width: 42px;
          height: 42px;
          flex: 0 0 auto;
          align-items: center;
          justify-content: center;
          border-radius: 8px;
          font-size: 21px;
        }

        .metric-icon.blue { color: #1d4ed8; background: rgba(37, 99, 235, 0.12); }
        .metric-icon.green { color: #047857; background: rgba(16, 185, 129, 0.12); }
        .metric-icon.amber { color: #b45309; background: rgba(245, 158, 11, 0.14); }
        .metric-icon.slate { color: #475569; background: rgba(100, 116, 139, 0.14); }

        .metric-item p,
        .metric-item strong,
        .metric-item span {
          display: block;
          margin: 0;
        }

        .metric-item p,
        .metric-item span {
          color: var(--app-text-muted, #64748b);
          font-size: 12px;
        }

        .metric-item strong {
          margin: 6px 0;
          color: var(--app-text, #111827);
          font-size: 22px;
        }

        .content-grid {
          display: grid;
          grid-template-columns: minmax(0, 1.45fr) minmax(320px, 0.55fr);
          gap: 24px;
        }

        .module-section,
        .side-section {
          min-width: 0;
        }

        .side-section {
          padding-left: 24px;
          border-left: 1px solid var(--app-border, #dbe4ef);
        }

        .section-heading {
          display: flex;
          align-items: flex-end;
          justify-content: space-between;
          gap: 16px;
          margin-bottom: 14px;
        }

        .section-heading h2 {
          margin: 4px 0 0;
          color: var(--app-text, #111827);
          font-size: 20px;
        }

        .module-list {
          border-top: 1px solid var(--app-border, #dbe4ef);
        }

        .module-item {
          display: grid;
          grid-template-columns: 44px minmax(0, 1fr) auto;
          gap: 14px;
          align-items: center;
          min-height: 92px;
          padding: 16px 4px;
          border-bottom: 1px solid var(--app-border, #dbe4ef);
        }

        .module-index {
          color: var(--dashboard-secondary);
          font-size: 12px;
          font-weight: 800;
        }

        .module-item strong {
          color: var(--app-text, #111827);
        }

        .module-item p {
          margin: 6px 0 0;
          color: var(--app-text-muted, #64748b);
          line-height: 1.5;
        }

        .module-meta {
          text-align: right;
        }

        .module-meta span,
        .module-meta b {
          display: block;
        }

        .module-meta span {
          color: var(--app-text-muted, #64748b);
          font-size: 12px;
        }

        .module-meta b {
          margin-top: 6px;
          color: var(--dashboard-primary);
          font-size: 13px;
        }

        .focus-list {
          display: flex;
          flex-direction: column;
        }

        .focus-list div {
          display: grid;
          grid-template-columns: 24px minmax(0, 1fr) auto;
          gap: 10px;
          align-items: center;
          min-height: 52px;
          border-bottom: 1px solid var(--app-border, #dbe4ef);
        }

        .focus-list .el-icon {
          color: var(--dashboard-secondary);
        }

        .focus-list b {
          color: var(--app-text-muted, #94a3b8);
          font-size: 11px;
        }

        .entity-heading {
          margin-top: 28px;
        }

        .entity-cloud {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
        }

        @media (max-width: 1100px) {
          .metric-grid {
            grid-template-columns: repeat(2, minmax(0, 1fr));
          }

          .content-grid {
            grid-template-columns: 1fr;
          }

          .side-section {
            padding: 0;
            border: 0;
          }
        }

        @media (max-width: 720px) {
          .dashboard-header {
            align-items: flex-start;
            flex-direction: column;
          }

          .environment-badge {
            min-width: 0;
            width: 100%;
          }

          .metric-grid {
            grid-template-columns: 1fr;
          }

          .module-item {
            grid-template-columns: 36px minmax(0, 1fr);
          }

          .module-meta {
            grid-column: 2;
            text-align: left;
          }
        }
        </style>
        """;
    }

    private static string NormalizePage(string? value)
    {
        var page = value?.Trim() ?? string.Empty;
        if (!SupportedPages.Contains(page))
            throw new ArgumentException("Page must be Login or Dashboard.");
        return SupportedPages.First(item => item.Equals(page, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeStyle(string? value)
    {
        var style = value?.Trim();
        return style?.ToLowerInvariant() switch
        {
            "technology" => "Technology",
            "industrial" => "Industrial",
            "commerce" => "Commerce",
            "minimal" => "Minimal",
            _ => "Enterprise"
        };
    }

    private static (string Primary, string Secondary) GetPalette(string style)
    {
        return style switch
        {
            "Technology" => ("#2563eb", "#06b6d4"),
            "Industrial" => ("#0f766e", "#f59e0b"),
            "Commerce" => ("#1d4ed8", "#16a34a"),
            "Minimal" => ("#111827", "#64748b"),
            _ => ("#1d4ed8", "#0f766e")
        };
    }

    private static string? NormalizeColor(string? value)
    {
        var color = value?.Trim();
        if (string.IsNullOrWhiteSpace(color))
            return null;
        return global::System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$")
            ? color.ToLowerInvariant()
            : null;
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static void EnsureInsideDirectory(string root, string path)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved UI page path is outside the generated frontend project.");
    }
}
