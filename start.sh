#!/bin/bash

echo "=========================================="
echo "  CodeMaster 项目启动脚本"
echo "=========================================="
echo ""

# 检查是否已运行数据库迁移
if [ ! -f ".migrated" ]; then
    echo "[1/3] 首次运行，执行数据库迁移..."
    cd CodeMaster.Migrator
    dotnet run
    if [ $? -eq 0 ]; then
        cd ..
        touch .migrated
        echo "✓ 数据库迁移完成"
    else
        echo "✗ 数据库迁移失败，请检查数据库连接"
        exit 1
    fi
    echo ""
else
    echo "[1/3] 数据库已迁移，跳过此步骤"
    echo ""
fi

# 启动后端
echo "[2/3] 启动后端服务..."
cd CodeMaster.WebApi
dotnet run &
BACKEND_PID=$!
echo "✓ 后端服务已启动 (PID: $BACKEND_PID)"
echo "  Swagger: https://localhost:5001/swagger"
echo ""

# 等待后端启动
sleep 5

# 启动前端
echo "[3/3] 启动前端服务..."
cd ../CodeMaster.Vue

# 检查是否已安装依赖
if [ ! -d "node_modules" ]; then
    echo "  首次运行，安装前端依赖..."
    npm install
fi

npm run dev &
FRONTEND_PID=$!
echo "✓ 前端服务已启动 (PID: $FRONTEND_PID)"
echo ""

echo "=========================================="
echo "  CodeMaster 启动完成！"
echo "=========================================="
echo "  后端地址: https://localhost:5001"
echo "  前端地址: http://localhost:5173"
echo "  Swagger:  https://localhost:5001/swagger"
echo ""
echo "  默认账号: admin"
echo "  默认密码: admin123"
echo "=========================================="
echo ""
echo "按 Ctrl+C 停止所有服务"

# 等待用户中断
trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; echo ''; echo '服务已停止'; exit" INT
wait
