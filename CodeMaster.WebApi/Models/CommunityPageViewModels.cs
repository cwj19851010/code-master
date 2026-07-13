using CodeMaster.Application.Dtos.Community;
using CodeMaster.Core.Dtos;

namespace CodeMaster.WebApi.Models;

public class CommunityIndexViewModel
{
    public List<CommunityCategoryDto> Categories { get; set; } = new();

    public PagedResultDto<CommunityTopicDto> Topics { get; set; } = new();

    public long? CategoryId { get; set; }

    public string? Keyword { get; set; }
}

public class CommunityTopicViewModel
{
    public CommunityTopicDto Topic { get; set; } = new();

    public List<CommunityReplyDto> Replies { get; set; } = new();
}
