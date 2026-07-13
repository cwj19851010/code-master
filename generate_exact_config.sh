#!/bin/bash

echo '{'
echo '  "templateVersion": "1.0.0",'
echo '  "description": "CodeMaster 模板配置 - 精确文件列表",'
echo '  "sourceRoot": ".",'
echo '  "include": {'

# Core
echo '    "core": ['
find CodeMaster.Core -type f \( -name "*.cs" -o -name "*.csproj" \) | \
  grep -v "/obj/" | grep -v "/bin/" | \
  grep -v "ProjectStatus.cs" | grep -v "ProjectType.cs" | grep -v "DatabaseType.cs" | \
  while read file; do
    echo "      \"$file\","
  done | sed '$ s/,$//'
echo '    ],'

# Domain
echo '    "domain": ['
find CodeMaster.Domain -type f \( -name "*.cs" -o -name "*.csproj" \) | \
  grep -v "/obj/" | grep -v "/bin/" | \
  grep -v "/Project/" | while read file; do
    echo "      \"$file\","
  done | sed '$ s/,$//'
echo '    ],'

# Application
echo '    "application": ['
find CodeMaster.Application -type f \( -name "*.cs" -o -name "*.csproj" \) | \
  grep -v "/obj/" | grep -v "/bin/" | \
  grep -v "/Project/" | while read file; do
    echo "      \"$file\","
  done | sed '$ s/,$//'
echo '    ],'

# Infrastructure
echo '    "infrastructure": ['
find CodeMaster.Infrastructure -type f \( -name "*.cs" -o -name "*.csproj" \) | \
  grep -v "/obj/" | grep -v "/bin/" | while read file; do
    echo "      \"$file\","
  done | sed '$ s/,$//'
echo '    ],'

# WebApi
echo '    "webapi": ['
find CodeMaster.WebApi -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.json" \) | \
  grep -v "/obj/" | grep -v "/bin/" | \
  grep -v "ProjectController.cs" | while read file; do
    echo "      \"$file\","
  done | sed '$ s/,$//'
echo '    ],'

# Migrator
echo '    "migrator": ['
echo '      "CodeMaster.Migrator/Program.cs",'
find CodeMaster.Migrator/Persistence -type f -name "*.cs" 2>/dev/null | \
  grep -v "/obj/" | grep -v "/bin/" | while read file; do
    echo "      \"$file\","
  done
echo '      "CodeMaster.Migrator/SeedData/translations.json",'
echo '      "CodeMaster.Migrator/SeedData/template.sln",'
find CodeMaster.Migrator/Migrations -type f -name "*.cs" | \
  grep -v "/obj/" | grep -v "/bin/" | \
  grep -v "20260221090742_AddProjectManagement" | \
  grep -v "20260221100832_AddProjectTypeToProject" | while read file; do
    echo "      \"$file\","
  done
echo '      "CodeMaster.Migrator/appsettings.json",'
echo '      "CodeMaster.Migrator/CodeMaster.Migrator.csproj"'
echo '    ],'

# Frontend
echo '    "frontend": ['
find CodeMaster.Vue/src -type f \( -name "*.vue" -o -name "*.js" -o -name "*.ts" -o -name "*.scss" -o -name "*.css" \) | \
  grep -v "node_modules" | grep -v "dist" | \
  grep -v "/project/" | grep -v "Project" | while read file; do
    echo "      \"$file\","
  done
find CodeMaster.Vue/public -type f 2>/dev/null | while read file; do
  echo "      \"$file\","
done
echo '      "CodeMaster.Vue/index.html",'
echo '      "CodeMaster.Vue/package.json",'
echo '      "CodeMaster.Vue/vite.config.js",'
echo '      "CodeMaster.Vue/.env.development",'
echo '      "CodeMaster.Vue/.env.production"'
echo '    ],'

# Root
echo '    "root": ['
echo '      ".gitignore",'
echo '      "README.md"'
echo '    ]'

echo '  }'
echo '}'
