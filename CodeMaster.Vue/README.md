# CodeMaster Vue 前端项目

基于 Vue 3 + Vite + Element Plus 的现代化管理后台前端

## 技术栈

- **Vue 3** - 渐进式 JavaScript 框架
- **Vite** - 下一代前端构建工具
- **Element Plus** - 基于 Vue 3 的组件库
- **Vue Router** - 官方路由管理器
- **Pinia** - 新一代状态管理
- **Axios** - HTTP 客户端
- **Sass** - CSS 预处理器

## 项目结构

```
CodeMaster.Vue/
├── src/
│   ├── api/              # API 接口
│   ├── assets/           # 静态资源
│   ├── layout/           # 布局组件
│   ├── router/           # 路由配置
│   ├── stores/           # 状态管理
│   ├── styles/           # 全局样式
│   ├── utils/            # 工具函数
│   ├── views/            # 页面组件
│   │   ├── dashboard/    # 首页
│   │   └── system/       # 系统管理
│   │       ├── user/     # 用户管理
│   │       ├── role/     # 角色管理
│   │       ├── dept/     # 部门管理
│   │       └── menu/     # 菜单管理
│   ├── App.vue           # 根组件
│   └── main.js           # 入口文件
├── index.html            # HTML 模板
├── vite.config.js        # Vite 配置
└── package.json          # 项目配置
```

## 快速开始

### 1. 安装依赖

```bash
npm install
```

### 2. 启动开发服务器

```bash
npm run dev
```

访问 http://localhost:3000

### 3. 构建生产版本

```bash
npm run build
```

### 4. 预览生产构建

```bash
npm run preview
```

## 功能特性

- ✅ Vue 3 组合式 API
- ✅ 使用 JavaScript（非 TypeScript）
- ✅ Element Plus UI 组件库
- ✅ 响应式布局
- ✅ 动态路由
- ✅ 权限管理
- ✅ 主题切换
- ✅ 国际化支持

## 开发规范

### 组件命名

- 使用 PascalCase 命名组件文件
- 使用 kebab-case 命名组件标签

### 代码风格

- 使用组合式 API（Composition API）
- 使用 `<script setup>` 语法糖
- 使用 JavaScript（不使用 TypeScript）

### 目录规范

- `views/` - 页面级组件
- `components/` - 通用组件
- `api/` - API 接口定义
- `utils/` - 工具函数

## API 配置

后端 API 地址配置在 `vite.config.js` 中：

```javascript
server: {
  proxy: {
    '/api': {
      target: 'https://localhost:5001',
      changeOrigin: true,
      secure: false
    }
  }
}
```

## 浏览器支持

- Chrome >= 87
- Firefox >= 78
- Safari >= 14
- Edge >= 88

## 许可证

MIT License
