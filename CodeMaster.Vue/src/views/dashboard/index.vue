<template>
  <div class="dashboard-container">
    <section class="dashboard-hero">
      <div>
        <p class="eyebrow">CodeMaster Workspace</p>
        <h1>第一版上线工作台</h1>
        <p class="hero-copy">围绕项目建模、模板维护、代码生成和发布验证组织日常工作。</p>
      </div>
      <div class="hero-status">
        <el-icon><CircleCheck /></el-icon>
        <span>模板驱动生成</span>
      </div>
    </section>

    <div class="metric-grid">
      <el-card v-for="item in metrics" :key="item.title" shadow="never" class="metric-card">
        <div class="metric-icon" :style="{ color: item.color, background: item.bg }">
          <el-icon><component :is="item.icon" /></el-icon>
        </div>
        <div>
          <div class="metric-title">{{ item.title }}</div>
          <div class="metric-value">{{ item.value }}</div>
          <div class="metric-desc">{{ item.desc }}</div>
        </div>
      </el-card>
    </div>

    <div class="dashboard-grid">
      <el-card shadow="never" class="work-card">
        <template #header>
          <div class="card-header">
            <span>上线验收链路</span>
            <el-tag type="success" size="small">Release 1</el-tag>
          </div>
        </template>
        <div class="check-list">
          <div v-for="item in checklist" :key="item.title" class="check-item">
            <el-icon :class="item.state"><CircleCheck /></el-icon>
            <div>
              <strong>{{ item.title }}</strong>
              <span>{{ item.desc }}</span>
            </div>
          </div>
        </div>
      </el-card>

      <el-card shadow="never" class="work-card">
        <template #header>
          <div class="card-header">
            <span>生成器能力</span>
            <el-tag size="small">CodeGen</el-tag>
          </div>
        </template>
        <div class="capability-list">
          <div v-for="item in capabilities" :key="item">
            <span></span>
            {{ item }}
          </div>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script setup>
import { CircleCheck, Collection, Connection, DataAnalysis, Files } from '@element-plus/icons-vue'

const metrics = [
  {
    title: '项目建模',
    value: 'Project / Module / Entity',
    desc: '项目、模块、实体和字段集中维护',
    icon: Collection,
    color: '#2563eb',
    bg: 'rgba(37, 99, 235, 0.12)'
  },
  {
    title: '模板系统',
    value: 'DB Templates',
    desc: '页面、字段控件、子表模板从数据库读取',
    icon: Files,
    color: '#0f766e',
    bg: 'rgba(15, 118, 110, 0.12)'
  },
  {
    title: '生成链路',
    value: 'Full + Incremental',
    desc: '支持全量生成和字段级增量更新',
    icon: Connection,
    color: '#b7791f',
    bg: 'rgba(183, 121, 31, 0.14)'
  },
  {
    title: '发布校验',
    value: 'Build Ready',
    desc: 'CodeMaster 与生成项目构建验证',
    icon: DataAnalysis,
    color: '#16a34a',
    bg: 'rgba(22, 163, 74, 0.12)'
  }
]

const checklist = [
  { title: 'CodeMaster 构建', desc: 'WebApi、Migrator、Vue 构建通过', state: 'ok' },
  { title: '模板同步', desc: '模板变更同步种子数据和当前数据库脚本', state: 'ok' },
  { title: 'OrderManager 场景', desc: '主子表、select-table、多选、图片、富文本生成验证', state: 'ok' },
  { title: '客户端模式', desc: '服务器授权登录，本地 sidecar 执行生成和数据库动作', state: 'ok' }
]

const capabilities = [
  '常规 CRUD 页面：列表、新增、编辑、详情',
  '一对多主子表：Order -> OrderItem 典型场景',
  '字段控件：input、number、select、select-table、date、switch、image、editor',
  'ScriptSection 按节点分离，生成时聚合去重',
  'tree.json、fields.json 与生成页面保持一致'
]
</script>

<style scoped lang="scss">
.dashboard-container {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.dashboard-hero {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  padding: 28px 30px;
  border: 1px solid var(--app-border, #dbe4ef);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(37, 99, 235, 0.10), transparent 36%),
    linear-gradient(135deg, var(--app-surface, #fff), var(--app-surface-soft, #f8fafc));
  box-shadow: var(--app-card-shadow, 0 10px 30px rgba(31, 41, 55, 0.08));

  h1 {
    margin: 4px 0 8px;
    color: var(--app-text, #1f2937);
    font-size: 28px;
    font-weight: 700;
  }
}

.eyebrow {
  margin: 0;
  color: var(--app-primary, #2563eb);
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
}

.hero-copy {
  margin: 0;
  color: var(--app-text-muted, #64748b);
}

.hero-status {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border: 1px solid var(--app-border, #dbe4ef);
  border-radius: 8px;
  color: var(--app-accent, #0f766e);
  background: var(--app-surface, #fff);
  font-weight: 600;
  white-space: nowrap;
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.metric-card :deep(.el-card__body) {
  display: flex;
  gap: 14px;
  min-height: 128px;
}

.metric-icon {
  display: inline-flex;
  width: 42px;
  height: 42px;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border-radius: 8px;
  font-size: 22px;
}

.metric-title {
  color: var(--app-text-muted, #64748b);
  font-size: 13px;
}

.metric-value {
  margin-top: 6px;
  color: var(--app-text, #1f2937);
  font-size: 17px;
  font-weight: 700;
}

.metric-desc {
  margin-top: 8px;
  color: var(--app-text-muted, #64748b);
  line-height: 1.55;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) minmax(320px, 0.9fr);
  gap: 14px;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-weight: 700;
}

.check-list,
.capability-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.check-item {
  display: flex;
  gap: 12px;
  padding: 12px;
  border-radius: 8px;
  background: var(--app-surface-soft, #f8fafc);

  .el-icon {
    margin-top: 2px;
    color: var(--app-accent, #0f766e);
  }

  strong,
  span {
    display: block;
  }

  strong {
    color: var(--app-text, #1f2937);
  }

  span {
    margin-top: 4px;
    color: var(--app-text-muted, #64748b);
  }
}

.capability-list div {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--app-text, #1f2937);

  span {
    width: 8px;
    height: 8px;
    flex-shrink: 0;
    border-radius: 999px;
    background: var(--app-primary, #2563eb);
  }
}

@media (max-width: 1100px) {
  .metric-grid,
  .dashboard-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .dashboard-hero {
    align-items: flex-start;
    flex-direction: column;
  }

  .metric-grid,
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}
</style>
