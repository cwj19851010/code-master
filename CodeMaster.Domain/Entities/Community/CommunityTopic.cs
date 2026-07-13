using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Community;

[SugarTable("community_topics")]
public class CommunityTopic : EntityBase
{
    public long CategoryId { get; set; }

    public long UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Status { get; set; }

    public bool IsPinned { get; set; }

    public bool IsFeatured { get; set; }

    public int ViewCount { get; set; }

    public int ReplyCount { get; set; }

    public int LikeCount { get; set; }

    public DateTime? LastReplyTime { get; set; }
}
