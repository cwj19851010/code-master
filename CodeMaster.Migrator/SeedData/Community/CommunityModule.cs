using CodeMaster.Domain.Entities.Community;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.Community;

public class CommunityModule : ISeedModule
{
    public string ModuleName => "社区";

    public Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return Task.FromResult(true);
    }

    public Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        return Task.CompletedTask;
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        if (await dbContext.Set<CommunityCategory>().AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var categories = new List<CommunityCategory>
        {
            new()
            {
                Id = YitIdHelper.NextId(),
                Name = "产品建议",
                Slug = "ideas",
                Description = "讨论 CodeMaster 的产品方向、功能建议和体验改进。",
                Sort = 1,
                CreateTime = now
            },
            new()
            {
                Id = YitIdHelper.NextId(),
                Name = "问题反馈",
                Slug = "issues",
                Description = "反馈使用中的问题、生成异常、客户端和 MCP 调用错误。",
                Sort = 2,
                CreateTime = now
            },
            new()
            {
                Id = YitIdHelper.NextId(),
                Name = "模板与生成",
                Slug = "codegen",
                Description = "讨论模板、控件属性、增量生成、字段排序和子表生成。",
                Sort = 3,
                CreateTime = now
            },
            new()
            {
                Id = YitIdHelper.NextId(),
                Name = "展示区",
                Slug = "showcase",
                Description = "分享使用 CodeMaster 生成或构建的项目。",
                Sort = 4,
                CreateTime = now
            }
        };

        await dbContext.Set<CommunityCategory>().AddRangeAsync(categories);
        Console.WriteLine("  - 创建社区默认分类");
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>();
    }
}
