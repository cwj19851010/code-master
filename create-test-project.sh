#!/bin/bash

# 配置
PROJECT_NAME="MyTestApp"
PROJECT_PATH="D:/TestProjects/MyTestApp"
BACKEND_PORT=5280
FRONTEND_PORT=5283
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijc3Mzg2Mzk1MjIzMjUxNyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJhZG1pbiIsIk5pY2tOYW1lIjoi6LaF57qn566h55CG5ZGYIiwianRpIjoiM2ZhZTBiMGUtNjg1Yy00MmNkLTk4ZTQtNDhmNjdjMzNlZTU3IiwiVGVuYW50SWQiOiIwIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiYWRtaW4iLCJQZXJtaXNzaW9uIjpbInN5c3RlbTp0YXNrOmxpc3QiLCJzeXN0ZW06dGFza2xvZzpsaXN0Iiwic3lzdGVtOnVzZXI6bGlzdCIsInN5c3RlbTpyb2xlOmxpc3QiLCJzeXN0ZW06ZGVwdDpsaXN0Iiwic3lzdGVtOm1lbnU6bGlzdCIsInN5c3RlbTp0ZW5hbnQ6bGlzdCIsInN5c3RlbTpwb3N0Omxpc3QiLCJzeXN0ZW06ZGljdDp0eXBlOmxpc3QiLCJzeXN0ZW06bGFuZzpsaXN0Iiwic3lzdGVtOm9wZXJsb2c6bGlzdCIsInN5c3RlbTpsb2dpbmxvZzpsaXN0Iiwic3lzdGVtOmZpbGU6bGlzdCIsInN5c3RlbTp1c2VyOmNyZWF0ZSIsInN5c3RlbTp1c2VyOnVwZGF0ZSIsInN5c3RlbTp1c2VyOnZpZXciLCJzeXN0ZW06cm9sZTpjcmVhdGUiLCJzeXN0ZW06cm9sZTp1cGRhdGUiLCJzeXN0ZW06cm9sZTp2aWV3Iiwic3lzdGVtOmRlcHQ6Y3JlYXRlIiwic3lzdGVtOmRlcHQ6dXBkYXRlIiwic3lzdGVtOmRlcHQ6dmlldyIsInN5c3RlbTptZW51OmNyZWF0ZSIsInN5c3RlbTptZW51OnVwZGF0ZSIsInN5c3RlbTptZW51OnZpZXciLCJzeXN0ZW06dGVuYW50OmNyZWF0ZSIsInN5c3RlbTp0ZW5hbnQ6dXBkYXRlIiwic3lzdGVtOnRlbmFudDp2aWV3Iiwic3lzdGVtOmRpY3Q6dHlwZTpjcmVhdGUiLCJzeXN0ZW06ZGljdDp0eXBlOnVwZGF0ZSIsInN5c3RlbTpkaWN0OnR5cGU6dmlldyIsInN5c3RlbTpsYW5nOmNyZWF0ZSIsInN5c3RlbTpsYW5nOnVwZGF0ZSIsInN5c3RlbTpsYW5nOnZpZXciLCJzeXN0ZW06dXNlcjpleHBvcnQiLCJzeXN0ZW06dXNlcjppbXBvcnQiLCJzeXN0ZW06dXNlcjpkZWxldGUiLCJzeXN0ZW06cG9zdDpjcmVhdGUiLCJzeXN0ZW06cG9zdDp1cGRhdGUiLCJzeXN0ZW06cG9zdDp2aWV3Iiwic3lzdGVtOm1lbnU6ZGVsZXRlIiwibW9uaXRvcjpsb2dpbmxvZzp2aWV3IiwibW9uaXRvcjpvcGVybG9nOnZpZXciLCJzeXN0ZW06dGFza2xvZzp2aWV3Iiwic3lzdGVtOnJvbGU6ZGVsZXRlIiwic3lzdGVtOnJvbGU6ZXhwb3J0Iiwic3lzdGVtOnJvbGU6aW1wb3J0Iiwic3lzdGVtOnRlbmFudDpkZWxldGUiLCJzeXN0ZW06dGVuYW50OmV4cG9ydCIsInN5c3RlbTp0ZW5hbnQ6aW1wb3J0Iiwic3lzdGVtOmZpbGU6dmlldyIsInN5c3RlbTpmaWxlOnVwbG9hZCIsInN5c3RlbTpmaWxlOmRvd25sb2FkIiwic3lzdGVtOmZpbGU6ZGVsZXRlIiwic3lzdGVtOmZpbGU6ZXhwb3J0Iiwic3lzdGVtOmZpbGU6aW1wb3J0Iiwic3lzdGVtOnByb2plY3Q6dmlldyIsInN5c3RlbTpwcm9qZWN0OmNyZWF0ZSIsInN5c3RlbTpwcm9qZWN0OnVwZGF0ZSIsInN5c3RlbTpwcm9qZWN0OmRlbGV0ZSIsInN5c3RlbTpwcm9qZWN0OmluaXRpYWxpemUiLCJzeXN0ZW06cHJvamVjdDpzdGFydCIsInN5c3RlbTpwcm9qZWN0OnN0b3AiLCJzeXN0ZW06cHJvamVjdDpsaXN0Iiwic3lzdGVtOnByb2plY3Rtb2R1bGU6bGlzdCIsInN5c3RlbTptb2R1bGVlbnRpdHk6bGlzdCJdLCJEYXRhU2NvcGUiOiIxIiwiUG9zdERhdGFTY29wZSI6IjEiLCJEZXB0SWQiOiI3NzM4NjM5NTIwNzI3NzMiLCJEZXB0QW5jZXN0b3JzIjoiMCIsIklzQWRtaW4iOiJ0cnVlIiwiZXhwIjoxNzcxODI1MjQ4LCJpc3MiOiJDb2RlTWFzdGVyIiwiYXVkIjoiQ29kZU1hc3Rlci5XZWJBcGkifQ.O8hRcoiat1QrbPMjvT7JfhtoDneVrbZIxwBzZkDeLLI"

echo "=========================================="
echo "创建测试项目: $PROJECT_NAME"
echo "=========================================="

# 1. 创建项目
echo "[1/5] 创建项目..."
RESPONSE=$(curl -X POST http://localhost:5170/api/project/project/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"projectName\": \"$PROJECT_NAME\",
    \"projectPath\": \"$PROJECT_PATH\",
    \"description\": \"通过API创建的测试项目\",
    \"dbType\": \"Sqlite\",
    \"connectionString\": \"Data Source=$PROJECT_NAME.db\"
  }" -s)

echo "创建项目响应: $RESPONSE"

# 提取项目ID（假设返回格式为 {"code":200,"data":{"id":"123456"}...}）
PROJECT_ID=$(echo $RESPONSE | grep -oP '"id"\s*:\s*"\K[^"]+' | head -1)
echo "项目ID: $PROJECT_ID"

if [ -z "$PROJECT_ID" ]; then
    echo "错误：无法获取项目ID"
    exit 1
fi

# 2. 导出模板
echo "[2/5] 导出模板..."
curl -X POST http://localhost:5170/api/project/project/exporttemplate \
  -H "Authorization: Bearer $TOKEN" \
  -s > /dev/null

# 3. 初始化项目
echo "[3/5] 初始化项目..."
INIT_RESPONSE=$(curl -X POST http://localhost:5170/api/project/project/initialize \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"projectId\": \"$PROJECT_ID\",
    \"backendPort\": $BACKEND_PORT,
    \"frontendPort\": $FRONTEND_PORT
  }" -s)

echo "初始化响应: $INIT_RESPONSE"

# 4. 安装前端依赖
echo "[4/5] 安装前端依赖..."
cd "$PROJECT_PATH/${PROJECT_NAME}.Vue" && npm install

# 5. 启动服务
echo "[5/5] 启动服务..."
echo "后端端口: $BACKEND_PORT"
echo "前端端口: $FRONTEND_PORT"

echo "=========================================="
echo "项目创建完成！"
echo "项目路径: $PROJECT_PATH"
echo "后端: http://localhost:$BACKEND_PORT"
echo "前端: http://localhost:$FRONTEND_PORT"
echo "=========================================="
