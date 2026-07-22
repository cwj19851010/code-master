import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const vueRoot = resolve(__dirname, '..')
const rawVersion = process.argv[2] || process.env.RELEASE_VERSION || ''
const version = rawVersion.trim().replace(/^v/i, '')

if (!/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(version)) {
  throw new Error(`Invalid release version: ${rawVersion || '(empty)'}`)
}

updateJson(resolve(vueRoot, 'package.json'), (content) => {
  content.version = version
})

updateJson(resolve(vueRoot, 'package-lock.json'), (content) => {
  content.version = version
  if (content.packages?.['']) {
    content.packages[''].version = version
  }
})

updateJson(resolve(vueRoot, 'src-tauri', 'tauri.conf.json'), (content) => {
  content.version = version
})

const cargoPath = resolve(vueRoot, 'src-tauri', 'Cargo.toml')
const cargoContent = readFileSync(cargoPath, 'utf8')
const cargoVersionPattern = /(\[package\][\s\S]*?\nversion\s*=\s*)"[^"]+"/

if (!cargoVersionPattern.test(cargoContent)) {
  throw new Error(`Could not update package version in: ${cargoPath}`)
}

const updatedCargoContent = cargoContent.replace(cargoVersionPattern, `$1"${version}"`)
writeFileSync(cargoPath, updatedCargoContent)
console.log(`Prepared CodeMaster Client release version ${version}`)

function updateJson(filePath, update) {
  if (!existsSync(filePath)) {
    throw new Error(`Required release file was not found: ${filePath}`)
  }

  const content = JSON.parse(readFileSync(filePath, 'utf8'))
  update(content)
  writeFileSync(filePath, `${JSON.stringify(content, null, 2)}\n`)
}
