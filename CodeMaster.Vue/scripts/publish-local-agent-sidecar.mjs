import { copyFileSync, cpSync, existsSync, mkdirSync, readdirSync, rmSync, statSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { spawnSync } from 'node:child_process'

const __dirname = dirname(fileURLToPath(import.meta.url))
const vueRoot = resolve(__dirname, '..')
const repoRoot = resolve(vueRoot, '..')
const binariesDir = join(vueRoot, 'src-tauri', 'binaries')
const resourcesDir = join(vueRoot, 'src-tauri', 'resources')
const publishDir = join(vueRoot, '.tauri-sidecar-publish')

const targets = {
  'win32-x64': {
    rid: 'win-x64',
    triple: 'x86_64-pc-windows-msvc',
    source: 'CodeMaster.LocalAgent.exe',
    extension: '.exe'
  },
  'win32-arm64': {
    rid: 'win-arm64',
    triple: 'aarch64-pc-windows-msvc',
    source: 'CodeMaster.LocalAgent.exe',
    extension: '.exe'
  },
  'darwin-x64': {
    rid: 'osx-x64',
    triple: 'x86_64-apple-darwin',
    source: 'CodeMaster.LocalAgent',
    extension: ''
  },
  'darwin-arm64': {
    rid: 'osx-arm64',
    triple: 'aarch64-apple-darwin',
    source: 'CodeMaster.LocalAgent',
    extension: ''
  },
  'linux-x64': {
    rid: 'linux-x64',
    triple: 'x86_64-unknown-linux-gnu',
    source: 'CodeMaster.LocalAgent',
    extension: ''
  },
  'linux-arm64': {
    rid: 'linux-arm64',
    triple: 'aarch64-unknown-linux-gnu',
    source: 'CodeMaster.LocalAgent',
    extension: ''
  }
}

const target = targets[`${process.platform}-${process.arch}`]

if (!target) {
  throw new Error(`Unsupported sidecar publish platform: ${process.platform}-${process.arch}`)
}

mkdirSync(binariesDir, { recursive: true })
prepareBundledResources()
rmSync(publishDir, { recursive: true, force: true })
mkdirSync(publishDir, { recursive: true })

const projectPath = join(repoRoot, 'CodeMaster.LocalAgent', 'CodeMaster.LocalAgent.csproj')
const publish = spawnSync(
  'dotnet',
  [
    'publish',
    projectPath,
    '-c',
    'Release',
    '-r',
    target.rid,
    '--self-contained',
    'true',
    '-p:PublishSingleFile=true',
    '-p:PublishTrimmed=false',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-o',
    publishDir
  ],
  {
    cwd: repoRoot,
    stdio: 'inherit',
    shell: process.platform === 'win32'
  }
)

if (publish.status !== 0) {
  throw new Error(`dotnet publish failed with exit code ${publish.status}`)
}

const sourcePath = join(publishDir, target.source)
if (!existsSync(sourcePath)) {
  throw new Error(`Published sidecar was not found: ${sourcePath}`)
}

const outputPath = join(binariesDir, `codemaster-local-agent-${target.triple}${target.extension}`)
copyFileSync(sourcePath, outputPath)

console.log(`Published Tauri sidecar: ${outputPath}`)

function prepareBundledResources() {
  rmSync(resourcesDir, { recursive: true, force: true })
  copyLegacyCodeGeneratorTemplates()
  copyLatestProjectTemplate()
}

function copyLegacyCodeGeneratorTemplates() {
  const sourceTemplatesDir = join(repoRoot, 'CodeMaster.CodeGenerator', 'Templates')
  if (!existsSync(sourceTemplatesDir)) {
    throw new Error(`Legacy code generator templates were not found: ${sourceTemplatesDir}`)
  }

  const targetTemplatesDir = join(resourcesDir, 'CodeMaster.CodeGenerator', 'Templates')
  mkdirSync(targetTemplatesDir, { recursive: true })
  cpSync(sourceTemplatesDir, targetTemplatesDir, { recursive: true })
}

function copyLatestProjectTemplate() {
  const sourceTemplatesDir = join(repoRoot, 'Templates')
  if (!existsSync(sourceTemplatesDir)) {
    throw new Error(`Project templates directory was not found: ${sourceTemplatesDir}`)
  }

  const latestTemplate = readdirSync(sourceTemplatesDir)
    .filter((name) => /^CodeMaster_Template_.*\.zip$/i.test(name))
    .map((name) => {
      const filePath = join(sourceTemplatesDir, name)
      return {
        name,
        filePath,
        mtimeMs: statSync(filePath).mtimeMs
      }
    })
    .sort((left, right) => right.mtimeMs - left.mtimeMs)[0]

  if (!latestTemplate) {
    throw new Error(`No project template zip was found in: ${sourceTemplatesDir}`)
  }

  const targetTemplatesDir = join(resourcesDir, 'Templates')
  mkdirSync(targetTemplatesDir, { recursive: true })
  copyFileSync(latestTemplate.filePath, join(targetTemplatesDir, latestTemplate.name))
  console.log(`Bundled latest project template: ${latestTemplate.name}`)
}
