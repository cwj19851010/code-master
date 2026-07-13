# 代码生成功能测试指南

## 当前状态

✅ 后端服务运行：http://localhost:5170
✅ 前端服务运行：http://localhost:5173
✅ 代码生成 API 已就绪：`POST /api/codegen/moduleentity/generatecode/{id}`

## 测试步骤

### 1. 登录系统
- 访问：http://localhost:5173
- 使用管理员账号登录

### 2. 创建测试项目
1. 进入"代码管理 > 项目管理"
2. 点击"新增"创建测试项目
   - 项目名称：TestProject
   - 项目描述：测试项目
   - 项目路径：选择一个本地目录（如：D:/TestProject）
   - 后端端口：5280
   - 前端端口：5283

### 3. 创建测试模块
1. 进入"代码管理 > 代码模块"
2. 点击"新增"创建模块
   - 所属项目：TestProject
   - 模块名称：Customer
   - 模块描述：客户管理
   - 排序：1

### 4. 创建测试实体
1. 进入"代码管理 > 实体管理"
2. 点击"新增"创建实体
   - 所属模块：Customer
   - 实体名称：Customer
   - 实体描述：客户
   - 表名：sys_customer
   - 选项：勾选"数据权限"和"多租户"

3. 添加字段（至少添加以下字段）：

   **字段1：**
   - 字段名：CustomerName
   - 描述：客户名称
   - 数据类型：string
   - 最大长度：100
   - 选项：必填、可搜索

   **字段2：**
   - 字段名：ContactPhone
   - 描述：联系电话
   - 数据类型：string
   - 最大长度：20
   - 选项：可搜索

   **字段3：**
   - 字段名：Email
   - 描述：邮箱
   - 数据类型：string
   - 最大长度：100
   - 选项：唯一

   **字段4：**
   - 字段名：Status
   - 描述：状态
   - 数据类型：int
   - 默认值：1
   - 选项：必填

4. 点击"保存"

### 5. 生成代码
1. 在实体列表中找到刚创建的"Customer"实体
2. 点击"生成代��"按钮
3. 确认生成对话框
4. 等待生成完成（会显示"代码生成成功！"提示）

### 6. 验证生成的代码

生成的文件应该包括：

**后端文件：**
- `{ProjectPath}/TestProject.Domain/Entities/Customer/Customer.cs` - 实体类
- `{ProjectPath}/TestProject.Application/Dtos/Customer/CustomerDto.cs` - DTO 类
- `{ProjectPath}/TestProject.Application/Services/Customer/ICustomerService.cs` - 服务接口
- `{ProjectPath}/TestProject.Application/Services/Customer/CustomerService.cs` - 服务实现

**前端文件：**
- `{ProjectPath}/TestProject.Vue/src/api/customer/customer.js` - API 文件

### 7. 检查生成的代码

#### 后端实体类应包含：
- 所有定义的字段属性
- 数据注解（如 [Required]、[MaxLength]、[Column]）
- 继承自正确的基类（如 FullAuditedEntity）
- 多租户支持（如果勾选）

#### DTO 类应包含：
- EntityDto：所有字段
- CreateDto：创建时需要的字段
- UpdateDto：更新时需要的字段
- QueryDto：查询条件字段

#### Service 类应包含：
- 接口：继承 ICrudApplicationService
- 实现：继承 CrudApplicationService
- 标记 [DynamicApi] 特性

#### 前端 API 应包含：
- getPagedList：分页查询
- getById：获取详情
- create：创建
- update：更新
- deleteById：删除

## 已完成的功能

✅ 实体和字段一体化管理
✅ 字段变更追踪（新增/修改/删除）
✅ 事务性操作
✅ 后端代码生成（Entity/DTO/Service）
✅ 前端 API 代码生成
✅ 生成代码按钮和 UI

## 待完成的功能

⏳ 前端页面代码生成（index.vue/add.vue/edit.vue/detail.vue）
⏳ 数据库迁移代码生成
⏳ 菜单自动生成并写入目标项目数据库
⏳ 项目初始化功能
⏳ 模块同步到菜单功能

## 注意事项

1. 确保项目路径存在且有写入权限
2. 生成代码前确保实体至少有一个字段
3. 字段名和实体名应遵循 C# 命名规范（PascalCase）
4. 表名应遵循数据库命名规范（snake_case）
5. 生成代码后需要手动编译项目以验证代码正确性

## 故障排查

### 问题1：生成代码失败
- 检查项目路径是否正确
- 检查是否有文件写入权限
- 查看后端日志：`tail -f CodeMaster.WebApi/backend.log`

### 问题2：生成的代码有编译错误
- 检查字段数据类型是否正确
- 检查命名空间是否正确
- 检查是否缺少必要的 using 引用

### 问题3：前端 API 调用失败
- 检查后端服务是否运行
- 检查 API 路由是否正确
- 使用浏览器开发者工具查看网络请求
