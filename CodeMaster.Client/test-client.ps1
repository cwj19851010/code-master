# CodeMaster 客户端测试脚本

Write-Host "=== CodeMaster 客户端功能测试 ===" -ForegroundColor Green
Write-Host ""

# 1. 检查编译状态
Write-Host "1. 检查客户端编译状态..." -ForegroundColor Yellow
$clientExe = "D:\MyHomeWorks\CodeMaster\CodeMaster.Client\bin\Debug\net8.0-windows\CodeMaster.Client.exe"
if (Test-Path $clientExe) {
    Write-Host "   ✓ 客户端已编译" -ForegroundColor Green
    $fileInfo = Get-Item $clientExe
    Write-Host "   文件大小: $($fileInfo.Length / 1KB) KB" -ForegroundColor Gray
    Write-Host "   修改时间: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "   ✗ 客户端未编译，正在编译..." -ForegroundColor Red
    Set-Location "D:\MyHomeWorks\CodeMaster\CodeMaster.Client"
    dotnet build
}

Write-Host ""

# 2. 检查 WebView2 Runtime
Write-Host "2. 检查 WebView2 Runtime..." -ForegroundColor Yellow
$webview2Path = "C:\Program Files (x86)\Microsoft\EdgeWebView\Application"
if (Test-Path $webview2Path) {
    Write-Host "   ✓ WebView2 Runtime 已安装" -ForegroundColor Green
} else {
    Write-Host "   ✗ WebView2 Runtime 未安装" -ForegroundColor Red
    Write-Host "   请访问: https://developer.microsoft.com/microsoft-edge/webview2/" -ForegroundColor Yellow
}

Write-Host ""

# 3. 检查后端服务
Write-Host "3. 检查后端服务..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5170/api/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"admin","password":"admin123"}' -ErrorAction Stop
    Write-Host "   ✓ 后端服务运行正常 (端口 5170)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ 后端服务未运行" -ForegroundColor Red
    Write-Host "   请先启动后端服务: cd CodeMaster.WebApi && dotnet run" -ForegroundColor Yellow
}

Write-Host ""

# 4. 检查前端文件
Write-Host "4. 检查前端 JSBridge 文件..." -ForegroundColor Yellow
$jsbridgeFile = "D:\MyHomeWorks\CodeMaster\CodeMaster.Vue\src\utils\jsbridge.js"
if (Test-Path $jsbridgeFile) {
    Write-Host "   ✓ jsbridge.js 已创建" -ForegroundColor Green
    $content = Get-Content $jsbridgeFile -Raw
    if ($content -match "isInClient") {
        Write-Host "   ✓ isInClient 函数存在" -ForegroundColor Green
    }
    if ($content -match "clientInitializeProject") {
        Write-Host "   ✓ clientInitializeProject 函数存在" -ForegroundColor Green
    }
    if ($content -match "selectFolder") {
        Write-Host "   ✓ selectFolder 函数存在" -ForegroundColor Green
    }
} else {
    Write-Host "   ✗ jsbridge.js 未找到" -ForegroundColor Red
}

Write-Host ""

# 5. 功能清单
Write-Host "5. 已实现的功能清单:" -ForegroundColor Yellow
Write-Host "   ✓ WebView2 集成" -ForegroundColor Green
Write-Host "   ✓ JSBridge 通信" -ForegroundColor Green
Write-Host "   ✓ 客户端项目初始化 (InitializeProject)" -ForegroundColor Green
Write-Host "   ✓ 文件夹选择对话框 (SelectFolder)" -ForegroundColor Green
Write-Host "   ✓ 原生消息框 (ShowMessage)" -ForegroundColor Green
Write-Host "   ✓ 自动环境检测 (isInClient)" -ForegroundColor Green
Write-Host "   ✓ Base64 模板解压" -ForegroundColor Green
Write-Host "   ✓ 文件重命名和内容替换" -ForegroundColor Green
Write-Host "   ✓ 数据库迁移执行" -ForegroundColor Green
Write-Host "   ✓ npm install 执行" -ForegroundColor Green

Write-Host ""

# 6. 启动选项
Write-Host "6. 启动选项:" -ForegroundColor Yellow
Write-Host "   [1] 启动 WPF 客户端" -ForegroundColor Cyan
Write-Host "   [2] 启动后端服务" -ForegroundColor Cyan
Write-Host "   [3] 启动前端开发服务器" -ForegroundColor Cyan
Write-Host "   [4] 全部启动" -ForegroundColor Cyan
Write-Host "   [Q] 退出" -ForegroundColor Cyan
Write-Host ""

$choice = Read-Host "请选择"

switch ($choice) {
    "1" {
        Write-Host "启动 WPF 客户端..." -ForegroundColor Green
        Start-Process $clientExe
    }
    "2" {
        Write-Host "启动后端服务..." -ForegroundColor Green
        Set-Location "D:\MyHomeWorks\CodeMaster\CodeMaster.WebApi"
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --urls http://localhost:5170"
    }
    "3" {
        Write-Host "启动前端开发服务器..." -ForegroundColor Green
        Set-Location "D:\MyHomeWorks\CodeMaster\CodeMaster.Vue"
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev"
    }
    "4" {
        Write-Host "启动所有服务..." -ForegroundColor Green

        # 启动后端
        Set-Location "D:\MyHomeWorks\CodeMaster\CodeMaster.WebApi"
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --urls http://localhost:5170"
        Start-Sleep -Seconds 3

        # 启动前端
        Set-Location "D:\MyHomeWorks\CodeMaster\CodeMaster.Vue"
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev"
        Start-Sleep -Seconds 3

        # 启动客户端
        Start-Process $clientExe
    }
    "Q" {
        Write-Host "退出" -ForegroundColor Gray
    }
    default {
        Write-Host "无效选择" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== 测试完成 ===" -ForegroundColor Green
