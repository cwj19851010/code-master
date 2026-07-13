using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Community;

public class CommunityCategoryDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class CommunityTopicDto
{
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public long UserId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Status { get; set; }

    public bool IsPinned { get; set; }

    public bool IsFeatured { get; set; }

    public int ViewCount { get; set; }

    public int ReplyCount { get; set; }

    public int LikeCount { get; set; }

    public DateTime CreateTime { get; set; }
}

public class CommunityTopicQueryDto : PagedQueryDto
{
    public long? CategoryId { get; set; }

    public string? Keyword { get; set; }
}

public class CreateCommunityTopicDto
{
    public long CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public class CommunityReplyDto
{
    public long Id { get; set; }

    public long TopicId { get; set; }

    public long UserId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsAccepted { get; set; }

    public int LikeCount { get; set; }

    public DateTime CreateTime { get; set; }
}

public class CreateCommunityReplyDto
{
    public long TopicId { get; set; }

    public long? ParentReplyId { get; set; }

    public string Content { get; set; } = string.Empty;
}
