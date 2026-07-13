using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Community;

[SugarTable("community_categories")]
public class CommunityCategory : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Sort { get; set; }

    public bool IsEnabled { get; set; } = true;
}
