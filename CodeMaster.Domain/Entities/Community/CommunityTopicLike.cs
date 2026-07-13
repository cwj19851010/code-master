using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Community;

[SugarTable("community_topic_likes")]
public class CommunityTopicLike : EntityBase
{
    public long TopicId { get; set; }

    public long UserId { get; set; }
}
