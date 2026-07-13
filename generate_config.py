import os
import json

def get_files(directory, extensions):
    """获取指定目录下的所有文件"""
    files = []
    for root, dirs, filenames in os.walk(directory):
        # 排除特定目录
        dirs[:] = [d for d in dirs if d not in ['bin', 'obj', 'node_modules', 'dist', '.vs', '.git']]
        
        for filename in filenames:
            if any(filename.endswith(ext) for ext in extensions):
                rel_path = os.path.relpath(os.path.join(root, filename), '.').replace('\', '/')
                files.append(rel_path)
    return sorted(files)

# 定义要包含的文件
config = {
    "templateVersion": "1.0.0",
    "description": "CodeMaster 模板配置 - 列出要包含在模板中的所有文件",
    "sourceRoot": ".",
    "include": {
        "core": get_files("CodeMaster.Core", [".cs", ".csproj"]),
        "domain": get_files("CodeMaster.Domain", [".cs", ".csproj"]),
        "application": get_files("CodeMaster.Application", [".cs", ".csproj"]),
        "infrastructure": get_files("CodeMaster.Infrastructure", [".cs", ".csproj"]),
        "webapi": get_files("CodeMaster.WebApi", [".cs", ".csproj", ".json"]),
        "migrator": [
            "CodeMaster.Migrator/Program.cs",
            "CodeMaster.Migrator/Persistence/EfCore/CodeMasterDbContext.cs",
            "CodeMaster.Migrator/Persistence/EfCore/CodeMasterDbContextFactory.cs",
            "CodeMaster.Migrator/SeedData/translations.json",
            "CodeMaster.Migrator/SeedData/template.sln",
            "CodeMaster.Migrator/Migrations/*.cs",
            "CodeMaster.Migrator/appsettings.json",
            "CodeMaster.Migrator/CodeMaster.Migrator.csproj"
        ],
        "frontend": get_files("CodeMaster.Vue/src", [".vue", ".js", ".ts", ".tsx", ".scss", ".css"]) + 
                   get_files("CodeMaster.Vue/public", [".html", ".ico", ".png", ".jpg", ".svg"]) +
                   ["CodeMaster.Vue/index.html", "CodeMaster.Vue/package.json", "CodeMaster.Vue/vite.config.js",
                    "CodeMaster.Vue/.env.development", "CodeMaster.Vue/.env.production"],
        "root": [".gitignore", "README.md"]
    },
    "exclude": {
        "migrations": [
            "CodeMaster.Migrator/Migrations/20260221090742_AddProjectManagement.cs",
            "CodeMaster.Migrator/Migrations/20260221090742_AddProjectManagement.Designer.cs",
            "CodeMaster.Migrator/Migrations/20260221100832_AddProjectTypeToProject.cs",
            "CodeMaster.Migrator/Migrations/20260221100832_AddProjectTypeToProject.Designer.cs"
        ]
    }
}

# 排除项目管理相关的文件
def filter_project_files(files):
    """排除项目管理相关的文件"""
    excluded_patterns = [
        'Project/',
        'project/',
        'ProjectController',
        'ProjectDto',
        'ProjectService',
        'ProjectStatus',
        'ProjectType',
        'DatabaseType'
    ]
    return [f for f in files if not any(pattern in f for pattern in excluded_patterns)]

# 过滤各个模块
config['include']['application'] = filter_project_files(config['include']['application'])
config['include']['webapi'] = filter_project_files(config['include']['webapi'])
config['include']['frontend'] = filter_project_files(config['include']['frontend'])

# 写入配置文件
with open('template-config.json', 'w', encoding='utf-8') as f:
    json.dump(config, f, indent=2, ensure_ascii=False)

print(f"配置文件已生成！")
print(f"Core: {len(config['include']['core'])} 个文件")
print(f"Domain: {len(config['include']['domain'])} 个文件")
print(f"Application: {len(config['include']['application'])} 个文件")
print(f"Infrastructure: {len(config['include']['infrastructure'])} 个文件")
print(f"WebApi: {len(config['include']['webapi'])} 个文件")
print(f"Migrator: {len(config['include']['migrator'])} 个文件")
print(f"Frontend: {len(config['include']['frontend'])} 个文件")
print(f"Root: {len(config['include']['root'])} 个文件")
print(f"总计: {sum(len(v) if isinstance(v, list) else 0 for v in config['include'].values())} 个文件")
