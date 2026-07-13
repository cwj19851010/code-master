# CodeMaster 客户端

CodeMaster 的 WPF 桌面客户端，集成 WebView2 用于显示 Vue 前端，并通过 JSBridge 实现客户端功能。

## 功能特性

### 1. WebView2 集成
- 内嵌浏览器显示 CodeMaster Vue 前端
- 支持开发者工具调试
- 自定义导航栏

### 2. JSBridge 功能
客户端通过 JSBridge 向前端提供以下功能：

#### `InitializeProject(jsonData)`
在客户端本地初始化项目
- 从 Base64 解码模板 ZIP
- 解压到本地目录
- 重命名文件和目录
- 替换项目名称和命名空间
- 更新数据库配置
- 运行 EF 数据库迁移
- 运行 npm install

#### `SelectFolder()`
打开文件夹选择对话框，返回选择的路径

#### `ShowMessage(message, title)`
显示原生消息框

## 使用方式

### 启动客户端

```bash
cd CodeMaster.Client
dotnet run
```

或直接运行编译后的 exe：
```
CodeMaster.Client\bin\Debug\net8.0-windows\CodeMaster.Client.exe
```

### 前端调用示例

```javascript
import { isInClient, clientInitializeProject, selectFolder, showMessage } from '@/utils/jsbridge'

// 检测是否在客户端环境
if (isInClient()) {
  console.log('运行在客户端 WebView 中')
} else {
  console.log('运行在浏览器中')
}

// 选择文件夹
const folderPath = await selectFolder()
console.log('选择的路径:', folderPath)

// 初始化项目
const data = {
  id: 123456,
  templateBase64: 'UEsDBBQAAA...',
  projectName: 'MyProject',
  projectPath: 'D:/Projects/MyProject',
  databaseType: 'SqlServer',
  connectionString: 'Server=localhost;Database=MyProjectDb;...'
}

const result = await clientInitializeProject(data)
if (result.success) {
  await showMessage('项目初始化成功！', '成功')
} else {
  await showMessage('初始化失败: ' + result.message, '错误')
}
```

## 双模式架构

CodeMaster 支持两种运行模式：

### 浏览器模式（服务端初始化）
- 前端运行在浏览器中
- 调用后端 API 在服务器上初始化项目
- 适合服务器端部署

### 客户端模式（本地初始化）
- 前端运行在 WPF WebView2 中
- 通过 JSBridge 在客户端本地初始化项目
- 适合桌面应用场景

前端会自动检测运行环境并选择相应的初始化方式。

## 技术栈

- .NET 8.0
- WPF (Windows Presentation Foundation)
- WebView2 (Microsoft Edge WebView2)
- Newtonsoft.Json

## 依赖项

```xml
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

## 开发说明

### 添加新的 JSBridge 方法

1. 在 `MainWindow.xaml.cs` 的 `JsBridge` 类中添加方法：

```csharp
[System.Runtime.InteropServices.ComVisible(true)]
public class JsBridge
{
    public string YourNewMethod(string param)
    {
        // 实现逻辑
        return JsonConvert.SerializeObject(new { success = true });
    }
}
```

2. 在前端 `jsbridge.js` 中添加调用封装：

```javascript
export async function yourNewMethod(param) {
  if (!isInClient()) {
    throw new Error('当前不在客户端环境中')
  }

  const result = await window.chrome.webview.hostObjects.jsbridge.YourNewMethod(param)
  return result
}
```

### 调试技巧

1. 点击"开发者工具"按钮打开 WebView2 开发者工具
2. 在控制台中测试 JSBridge：
```javascript
// 检测 JSBridge 是否可用
console.log(window.chrome.webview.hostObjects.jsbridge)

// 测试方法调用
await window.chrome.webview.hostObjects.jsbridge.ShowMessage('测试', '标题')
```

## 注意事项

1. **COM 可见性**：所有 JSBridge 方法必须标记 `[ComVisible(true)]`
2. **UI 线程**：涉及 UI 操作的方法需要使用 `Dispatcher.Invoke`
3. **异步调用**：前端调用 JSBridge 方法时使用 `await`
4. **错误处理**：JSBridge 方法应返回 JSON 格式的结果，包含 `success` 和 `message` 字段
5. **路径格式**：Windows 路径使用反斜杠 `\` 或正斜杠 `/` 都可以

## 项目结构

```
CodeMaster.Client/
├── MainWindow.xaml          # 主窗口 UI
├── MainWindow.xaml.cs       # 主窗口逻辑 + JSBridge
├── App.xaml                 # 应用程序配置
├── App.xaml.cs              # 应用程序入口
└── CodeMaster.Client.csproj # 项目配置
```

## 许可证

与 CodeMaster 主项目相同
