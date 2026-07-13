using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Community;

[SugarTable("community_replies")]
public class CommunityReply : EntityBase
{
    public long TopicId { get; set; }

    public long UserId { get; set; }

    public long? ParentReplyId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsAccepted { get; set; }

    public int LikeCount { get; set; }
}
