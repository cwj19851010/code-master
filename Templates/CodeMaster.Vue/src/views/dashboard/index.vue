<template>
  <div class="dashboard-container">
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#409eff" :size="40">
              <User />
            </el-icon>
            <div class="stat-content">
              <div class="stat-title">{{ t('total_users') }}</div>
              <div class="stat-value">1,234</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#67c23a" :size="40">
              <UserFilled />
            </el-icon>
            <div class="stat-content">
              <div class="stat-title">{{ t('total_roles') }}</div>
              <div class="stat-value">56</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#e6a23c" :size="40">
              <OfficeBuilding />
            </el-icon>
            <div class="stat-content">
              <div class="stat-title">{{ t('total_depts') }}</div>
              <div class="stat-value">89</div>
            </div>
          </div>
        </el-card>
      </el-col>
      
      <el-col :span="6">
        <el-card shadow="hover">
          <div class="stat-card">
            <el-icon class="stat-icon" color="#f56c6c" :size="40">
              <Menu />
            </el-icon>
            <div class="stat-content">
              <div class="stat-title">{{ t('total_menus') }}</div>
              <div class="stat-value">123</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    
    <el-row :gutter="20" class="mt-20">
      <el-col :span="24">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>{{ t('welcome_use_app') }}</span>
            </div>
          </template>
          <div class="welcome-content">
            <h2>{{ t('welcome_title') }}</h2>
            <p>{{ t('welcome_desc') }}</p>
            <el-divider />
            <h3>{{ t('core_features') }}</h3>
            <ul>
              <li>{{ t('feature_ddd') }}</li>
              <li>{{ t('feature_orm') }}</li>
              <li>{{ t('feature_rbac') }}</li>
              <li>{{ t('feature_codegen') }}</li>
              <li>{{ t('feature_multitenant') }}</li>
              <li>{{ t('feature_ui') }}</li>
            </ul>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<style scoped lang="scss">
.dashboard-container {
  .stat-card {
    display: flex;
    align-items: center;
    
    .stat-icon {
      margin-right: 20px;
    }
    
    .stat-content {
      flex: 1;
      
      .stat-title {
        font-size: 14px;
        color: #909399;
        margin-bottom: 10px;
      }
      
      .stat-value {
        font-size: 28px;
        font-weight: bold;
        color: #303133;
      }
    }
  }
  
  .welcome-content {
    h2 {
      color: #409eff;
      margin-bottom: 10px;
    }
    
    p {
      color: #606266;
      font-size: 16px;
      margin-bottom: 20px;
    }
    
    h3 {
      color: #303133;
      margin-bottom: 15px;
    }
    
    ul {
      list-style: none;
      padding: 0;
      
      li {
        padding: 8px 0;
        font-size: 15px;
        color: #606266;
      }
    }
  }
}
</style>
