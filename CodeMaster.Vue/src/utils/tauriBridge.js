export function isTauriRuntime() {
  return Boolean(window.__TAURI_INTERNALS__ || window.__TAURI__)
}

export async function installTauriBridge() {
  if (!isTauriRuntime() || window.CodeMasterBridge?.invoke) {
    return
  }

  const { invoke } = await import('@tauri-apps/api/core')

  window.CodeMasterBridge = {
    invoke(channel, payload) {
      if (channel !== 'codegenExecution') {
        return Promise.reject(new Error(`Unsupported CodeMaster bridge channel: ${channel}`))
      }

      return invoke('codegen_execution', { request: payload })
    }
  }

  window.__CODEMASTER_CLIENT__ = {
    ...(window.__CODEMASTER_CLIENT__ || {}),
    localExecution: true
  }
}
