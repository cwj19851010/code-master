using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen;

internal static class GeneratedTemplateCleanup
{
    public static string RemoveLoginClientBridge(string content)
    {
        var result = content;

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*<el-form-item\s+prop=""serverBaseUrl"">.*?^[ \t]*</el-form-item>[ \t]*(?:\r?\n)?",
            string.Empty);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*<div\s+class=""auth-links"">.*?^[ \t]*</div>[ \t]*(?:\r?\n)?",
            string.Empty);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*import\s*\{(?:[^\r\n]|\r?\n(?![ \t]*import\b))*?\}\s*from\s*['""]@/utils/codegenExecution['""][ \t]*(?:\r?\n)?",
            string.Empty);

        result = Regex.Replace(result, @"(?m)^import\s*\{\s*ref\s*,\s*reactive\s*,\s*onMounted\s*\}\s*from\s*['""]vue['""][ \t]*\r?$", "import { ref, reactive } from 'vue'");
        result = Regex.Replace(result, @"(?m)^import\s*\{\s*useRoute\s*,\s*useRouter\s*\}\s*from\s*['""]vue-router['""][ \t]*\r?$", "import { useRouter } from 'vue-router'");
        result = Regex.Replace(result, @"(?m)^[ \t]*import\s*\{\s*getToken\s*\}\s*from\s*['""]@/utils/auth['""][ \t]*(?:\r?\n)?", string.Empty);

        result = Regex.Replace(result, @"(?m)^\s*const\s+clientConfig\s*=.*\r?\n", string.Empty);
        result = Regex.Replace(result, @"(?m)^\s*const\s+isClientLogin\s*=.*\r?\n", "const isClientLogin = false" + Environment.NewLine);
        result = Regex.Replace(result, @"(?m)^\s*const\s+route\s*=\s*useRoute\(\)\s*\r?\n", string.Empty);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*const\s+defaultServerBaseUrl\s*=\s*normalize\w*ServerBaseUrl\(\s*.*?^[ \t]*\)[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        if (result.Contains("isClientLogin", StringComparison.Ordinal) &&
            !Regex.IsMatch(result, @"(?m)^\s*const\s+isClientLogin\s*="))
        {
            result = Regex.Replace(
                result,
                @"(?m)^(\s*const\s+\{\s*t\s*\}\s*=\s*useI18n\(\)\s*\r?\n)",
                "$1const isClientLogin = false" + Environment.NewLine,
                RegexOptions.None,
                TimeSpan.FromSeconds(1));
        }

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*serverBaseUrl\s*:\s*\[\s*\{.*?normalize\w*ServerBaseUrl.*?^[ \t]*\][ \t]*,?[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(result, @"(?m)^\s*serverBaseUrl\s*:.*\r?\n", string.Empty);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*const\s+serverBaseUrl\s*=\s*normalize\w*ServerBaseUrl\(loginForm\.serverBaseUrl\)\s*save\w*ClientConfig\(\{\s*.*?^[ \t]*\}\)[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*function\s+getPublicUrl\(path\)\s*\{.*?^[ \t]*\}[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*function\s+openRegister\(\)\s*\{.*?^[ \t]*\}[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*function\s+openGithubLogin\(\)\s*\{.*?^[ \t]*\}[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^[ \t]*async\s+function\s+completeExternalLogin\(\)\s*\{.*?^[ \t]*\}[ \t]*(?:\r?\n)+(?=const\s+handleLogin\s*=)",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^onMounted\(\(\)\s*=>\s*\{.*?^\}\)[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = Regex.Replace(
            result,
            @"(?ms)^\.auth-links\s*\{.*?^\}[ \t]*(?:\r?\n)?",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.Multiline);

        result = EnsureLoginBaseImports(result);

        return Regex.Replace(result, @"\r?\n{3,}", Environment.NewLine + Environment.NewLine);
    }

    private static string EnsureLoginBaseImports(string content)
    {
        var result = content;

        result = EnsureScriptSetupImport(result, "from 'vue'", "import { ref, reactive } from 'vue'");
        result = EnsureScriptSetupImport(result, "from 'vue-router'", "import { useRouter } from 'vue-router'");
        result = EnsureScriptSetupImport(result, "from 'element-plus'", "import { ElMessage } from 'element-plus'");
        result = EnsureScriptSetupImport(result, "from '@element-plus/icons-vue'", "import { User, Lock, View, Hide, Platform } from '@element-plus/icons-vue'");
        result = EnsureScriptSetupImport(result, "from '@/stores/user'", "import { useUserStore } from '@/stores/user'");
        result = EnsureScriptSetupImport(result, "from 'vue-i18n'", "import { useI18n } from 'vue-i18n'");
        result = EnsureScriptSetupImport(result, "from '@/i18n'", "import { t2 } from '@/i18n'");
        result = EnsureScriptSetupImport(result, "ThemePicker from '@/layout/components/ThemePicker.vue'", "import ThemePicker from '@/layout/components/ThemePicker.vue'");

        return result;
    }

    private static string EnsureScriptSetupImport(string content, string identity, string importLine)
    {
        if (content.Contains(identity, StringComparison.Ordinal))
        {
            return content;
        }

        return Regex.Replace(
            content,
            @"<script setup>\s*",
            match => match.Value + importLine + Environment.NewLine,
            RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }
}
