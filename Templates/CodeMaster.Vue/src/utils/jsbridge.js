/**
 * JSBridge 工具类
 * 用于检测运行环境（浏览器 vs 客户端 WebView）并调用相应的初始化方法
 */

/**
 * 检测是否在客户端 WebView 中运行
 */
export function isInClient() {
  return typeof window.chrome !== 'undefined' &&
         typeof window.chrome.webview !== 'undefined' &&
         typeof window.chrome.webview.hostObjects !== 'undefined' &&
         typeof window.chrome.webview.hostObjects.jsbridge !== 'undefined'
}

/**
 * 客户端初始化项目（通过 JSBridge）
 * @param {Object} data - 初始化数据
 * @param {number} data.id - 项目ID
 * @param {string} data.templateBase64 - 模板 Base64
 * @param {string} data.projectName - 项目名称
 * @param {string} data.projectPath - 项目路径
 * @param {string} data.databaseType - 数据库类型
 * @param {string} data.connectionString - 连接字符串
 * @returns {Promise<Object>} 初始化结果
 */
export async function clientInitializeProject(data) {
  if (!isInClient()) {
    throw new Error('当前不在客户端环境中')
  }

  try {
    // 调用 C# 的 JsBridge.InitializeProject 方法
    const jsonData = JSON.stringify(data)
    const resultJson = await window.chrome.webview.hostObjects.jsbridge.InitializeProject(jsonData)
    const result = JSON.parse(resultJson)

    if (!result.success) {
      throw new Error(result.message)
    }

    return result
  } catch (error) {
    console.error('客户端初始化失败:', error)
    throw error
  }
}

/**
 * 选择文件夹（客户端功能）
 * @returns {Promise<string>} 选择的文件夹路径
 */
export async function selectFolder() {
  if (!isInClient()) {
    throw new Error('当前不在客户端环境中')
  }

  try {
    const path = await window.chrome.webview.hostObjects.jsbridge.SelectFolder()
    return path
  } catch (error) {
    console.error('选择文件夹失败:', error)
    throw error
  }
}

/**
 * 显示消息框（客户端功能）
 * @param {string} message - 消息内容
 * @param {string} title - 标题
 */
export async function showMessage(message, title = '提示') {
  if (!isInClient()) {
    alert(message)
    return
  }

  try {
    await window.chrome.webview.hostObjects.jsbridge.ShowMessage(message, title)
  } catch (error) {
    console.error('显示消息失败:', error)
    alert(message)
  }
}

/**
 * 同步模块到菜单（客户端模式）
 * @param {Object} data - 同步数据
 * @param {number} data.moduleId - 模块ID
 * @param {number} data.projectId - 项目ID
 * @param {string} data.projectName - 项目名称
 * @param {string} data.projectPath - 项目路径
 * @param {string} data.databaseType - 数据库类型
 * @param {string} data.connectionString - 连接字符串
 * @param {string} data.moduleName - 模块名称（英文）
 * @param {string} data.moduleDescription - 模块描述（中文）
 * @param {string} data.icon - 图标
 * @param {number} data.orderNum - 排序号
 * @returns {Promise<Object>} 同步结果
 */
export async function syncModuleToMenu(data) {
  if (!isInClient()) {
    throw new Error('当前不在客户端环境中')
  }

  try {
    // 调用 C# 的 JsBridge.SyncModuleToMenu 方法
    const jsonData = JSON.stringify(data)
    const resultJson = await window.chrome.webview.hostObjects.jsbridge.SyncModuleToMenu(jsonData)
    const result = JSON.parse(resultJson)

    if (!result.success) {
      throw new Error(result.message)
    }

    return result
  } catch (error) {
    console.error('同步模块到菜单失败:', error)
    throw error
  }
}
