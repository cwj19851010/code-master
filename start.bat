@echo off
chcp 65001 >nul
echo ==========================================
echo   CodeMaster 项目启动脚本
echo ==========================================
echo.

REM 检查是否已运行数据库迁移
if not exist ".migrated" (
    echo [1/3] 首次运行，执行数据库迁移...
    cd CodeMaster.Migrator
    dotnet run
    if %errorlevel% equ 0 (
        cd ..
        echo. > .migrated
        echo ✓ 数据库迁移完成
    ) else (
        echo ✗ 数据库迁移失败，请检查数据库连接
        pause
        exit /b 1
    )
    echo.
) else (
    echo [1/3] 数据库已迁移，跳过此步骤
    echo.
)

REM 启动后端
echo [2/3] 启动后端服务...
cd CodeMaster.WebApi
start "CodeMaster Backend" dotnet run
echo ✓ 后端服务已启动
echo   Swagger: https://localhost:5001/swagger
echo.

REM 等待后端启动
timeout /t 5 /nobreak >nul

REM 启动前端
echo [3/3] 启动前端服务...
cd ..\CodeMaster.Vue

REM 检查是否已安装依赖
if not exist "node_modules" (
    echo   首次运行，安装前端依赖...
    call npm install
)

start "CodeMaster Frontend" npm run dev
echo ✓ 前端服务已启动
echo.

echo ==========================================
echo   CodeMaster 启动完成��
echo ==========================================
echo   后端地址: https://localhost:5001
echo   前端地址: http://localhost:5173
echo   Swagger:  https://localhost:5001/swagger
echo.
echo   默认账号: admin
echo   默认密码: admin123
echo ==========================================
echo.
echo 按任意键退出...
pause >nul
