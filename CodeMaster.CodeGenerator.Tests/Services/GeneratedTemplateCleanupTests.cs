using System.Reflection;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class GeneratedTemplateCleanupTests
{
    [Fact]
    public void RemoveLoginClientBridge_RemovesProjectNamedClientBridgeReferences()
    {
        const string source = """
<template>
  <el-form-item prop="serverBaseUrl">
    <el-input v-model="loginForm.serverBaseUrl" />
  </el-form-item>
  <div class="auth-links">
    <el-button text @click="openRegister">Register</el-button>
    <el-button text @click="openGithubLogin">GitHub</el-button>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getToken } from '@/utils/auth'
import {
  DEFAULT_ORDER_SYSTEM_SERVER_BASE_URL,
  getOrderSystemClientConfig,
  isOrderSystemClient,
  normalizeOrderSystemServerBaseUrl,
  saveOrderSystemClientConfig
} from '@/utils/codegenExecution'

const router = useRouter()
const route = useRoute()
const clientConfig = getOrderSystemClientConfig()
const isClientLogin = isOrderSystemClient()
const defaultServerBaseUrl = normalizeOrderSystemServerBaseUrl(
  clientConfig.serverBaseUrl ||
  window.__CODEMASTER_CLIENT__?.serverBaseUrl ||
  DEFAULT_ORDER_SYSTEM_SERVER_BASE_URL
)

const loginForm = reactive({
  serverBaseUrl: defaultServerBaseUrl,
  username: 'admin',
  password: 'admin123'
})

const loginRules = {
  serverBaseUrl: [
    {
      trigger: 'blur',
      validator: (_, value, callback) => {
        normalizeOrderSystemServerBaseUrl(value)
        callback()
      }
    }
  ],
  username: []
}

function getPublicUrl(path) {
  return new URL(path, defaultServerBaseUrl).toString()
}

function openRegister() {
  window.location.href = getPublicUrl('/register')
}

function openGithubLogin() {
  window.location.href = getPublicUrl('/api/account/github/login')
}

async function completeExternalLogin() {
  if (!route.query.github && !route.query.registered) {
    return
  }

  const token = getToken()
  if (token) {
    router.replace('/')
  }
}

const handleLogin = () => {
  const serverBaseUrl = normalizeOrderSystemServerBaseUrl(loginForm.serverBaseUrl)

  saveOrderSystemClientConfig({
    ...getOrderSystemClientConfig(),
    serverBaseUrl
  })

  router.push('/')
}

onMounted(() => {
  saveOrderSystemClientConfig({
    ...getOrderSystemClientConfig(),
    serverBaseUrl: defaultServerBaseUrl
  })
  completeExternalLogin()
})
</script>

<style scoped>
.auth-links {
  display: flex;
  justify-content: center;

  :deep(.el-button) {
    font-weight: 700;
  }
}
</style>
""";

        var cleaned = InvokeRemoveLoginClientBridge(source);

        var removedFragments = new[]
        {
            "normalizeOrderSystemServerBaseUrl",
            "codegenExecution",
            "DEFAULT_ORDER_SYSTEM_SERVER_BASE_URL",
            "getOrderSystemClientConfig",
            "saveOrderSystemClientConfig",
            "defaultServerBaseUrl",
            "serverBaseUrl",
            "openRegister",
            "openGithubLogin",
            "completeExternalLogin",
            "onMounted",
            "useRoute()",
            "const route = useRoute",
            "getToken",
            "auth-links"
        };

        foreach (var fragment in removedFragments)
        {
            Assert.DoesNotContain(fragment, cleaned);
        }

        Assert.Contains("const isClientLogin = false", cleaned);
        Assert.Contains("import { ref, reactive } from 'vue'", cleaned);
        Assert.Contains("import { useRouter } from 'vue-router'", cleaned);
        Assert.Contains("username: 'admin'", cleaned);
        Assert.Contains("password: 'admin123'", cleaned);
    }

    private static string InvokeRemoveLoginClientBridge(string content)
    {
        var type = typeof(CodeGeneratorService).Assembly.GetType(
            "CodeMaster.Application.Services.CodeGen.GeneratedTemplateCleanup",
            throwOnError: true)!;
        var method = type.GetMethod(
            "RemoveLoginClientBridge",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        return (string)method.Invoke(null, new object[] { content })!;
    }
}
