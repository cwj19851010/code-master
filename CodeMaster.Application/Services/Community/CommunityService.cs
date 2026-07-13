using System.Security.Claims;
using CodeMaster.Application.Dtos.Community;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.Community;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using Yitter.IdGenerator;

namespace CodeMaster.Application.Services.Community;

public interface ICommunityService
{
    Task<List<CommunityCategoryDto>> GetCategoriesAsync();

    Task<PagedResultDto<CommunityTopicDto>> GetTopicsAsync(CommunityTopicQueryDto query);

    Task<CommunityTopicDto?> GetTopicAsync(long id);

    Task<long> CreateTopicAsync(CreateCommunityTopicDto dto);

    Task<List<CommunityReplyDto>> GetRepliesAsync(long topicId);

    Task<long> CreateReplyAsync(CreateCommunityReplyDto dto);
}

public class CommunityService : ICommunityService
{
    private readonly IRepository<CommunityTopic> _topicRepository;
    private readonly IRepository<CommunityReply> _replyRepository;
    private readonly ISqlSugarClient _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommunityService(
        IRepository<CommunityTopic> topicRepository,
        IRepository<CommunityReply> replyRepository,
        ISqlSugarClient db,
        IHttpContextAccessor httpContextAccessor)
    {
        _topicRepository = topicRepository;
        _replyRepository = replyRepository;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<CommunityCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _db.Queryable<CommunityCategory>()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Sort)
            .ToListAsync();

        return categories.Select(x => new CommunityCategoryDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Description = x.Description
        }).ToList();
    }

    public async Task<PagedResultDto<CommunityTopicDto>> GetTopicsAsync(CommunityTopicQueryDto query)
    {
        var q = _db.Queryable<CommunityTopic>()
            .WhereIF(query.CategoryId.HasValue, x => x.CategoryId == query.CategoryId!.Value)
            .WhereIF(!string.IsNullOrWhiteSpace(query.Keyword), x => x.Title.Contains(query.Keyword!));

        var total = await q.CountAsync();
        var topics = await q
            .OrderByDescending(x => x.IsPinned)
            .OrderByDescending(x => x.LastReplyTime ?? x.CreateTime)
            .ToPageListAsync(query.PageNum, query.PageSize);

        var result = await MapTopicsAsync(topics);
        return new PagedResultDto<CommunityTopicDto>
        {
            Items = result,
            Total = total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    public async Task<CommunityTopicDto?> GetTopicAsync(long id)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
        {
            return null;
        }

        topic.ViewCount++;
        await _db.Updateable(topic).UpdateColumns(x => new { x.ViewCount }).ExecuteCommandAsync();

        return (await MapTopicsAsync(new List<CommunityTopic> { topic })).FirstOrDefault();
    }

    public async Task<long> CreateTopicAsync(CreateCommunityTopicDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length < 4)
        {
            throw new InvalidOperationException("Topic title is too short.");
        }
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Trim().Length < 10)
        {
            throw new InvalidOperationException("Topic content is too short.");
        }

        var categoryExists = await _db.Queryable<CommunityCategory>()
            .Where(x => x.Id == dto.CategoryId && x.IsEnabled)
            .AnyAsync();
        if (!categoryExists)
        {
            throw new InvalidOperationException("Community category does not exist.");
        }

        var topic = new CommunityTopic
        {
            Id = YitIdHelper.NextId(),
            CategoryId = dto.CategoryId,
            UserId = userId,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Status = 0,
            CreateTime = DateTime.UtcNow,
            LastReplyTime = DateTime.UtcNow
        };

        return await _topicRepository.InsertAsync(topic);
    }

    public async Task<List<CommunityReplyDto>> GetRepliesAsync(long topicId)
    {
        var replies = await _db.Queryable<CommunityReply>()
            .Where(x => x.TopicId == topicId)
            .OrderBy(x => x.CreateTime)
            .ToListAsync();
        var userIds = replies.Select(x => x.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<long, SysUser>()
            : (await _db.Queryable<SysUser>().Where(x => userIds.Contains(x.Id)).ToListAsync())
                .ToDictionary(x => x.Id);

        return replies.Select(x => new CommunityReplyDto
        {
            Id = x.Id,
            TopicId = x.TopicId,
            UserId = x.UserId,
            AuthorName = users.TryGetValue(x.UserId, out var user) ? user.NickName : "CodeMaster user",
            Content = x.Content,
            IsAccepted = x.IsAccepted,
            LikeCount = x.LikeCount,
            CreateTime = x.CreateTime
        }).ToList();
    }

    public async Task<long> CreateReplyAsync(CreateCommunityReplyDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Trim().Length < 2)
        {
            throw new InvalidOperationException("Reply content is too short.");
        }

        var topic = await _topicRepository.GetByIdAsync(dto.TopicId);
        if (topic == null)
        {
            throw new InvalidOperationException("Topic does not exist.");
        }

        var reply = new CommunityReply
        {
            Id = YitIdHelper.NextId(),
            TopicId = dto.TopicId,
            UserId = userId,
            ParentReplyId = dto.ParentReplyId,
            Content = dto.Content.Trim(),
            CreateTime = DateTime.UtcNow
        };

        var replyId = await _replyRepository.InsertAsync(reply);
        topic.ReplyCount++;
        topic.LastReplyTime = DateTime.UtcNow;
        await _db.Updateable(topic).UpdateColumns(x => new { x.ReplyCount, x.LastReplyTime }).ExecuteCommandAsync();
        return replyId;
    }

    private async Task<List<CommunityTopicDto>> MapTopicsAsync(List<CommunityTopic> topics)
    {
        var categoryIds = topics.Select(x => x.CategoryId).Distinct().ToList();
        var userIds = topics.Select(x => x.UserId).Distinct().ToList();
        var categories = categoryIds.Count == 0
            ? new Dictionary<long, CommunityCategory>()
            : (await _db.Queryable<CommunityCategory>().Where(x => categoryIds.Contains(x.Id)).ToListAsync())
                .ToDictionary(x => x.Id);
        var users = userIds.Count == 0
            ? new Dictionary<long, SysUser>()
            : (await _db.Queryable<SysUser>().Where(x => userIds.Contains(x.Id)).ToListAsync())
                .ToDictionary(x => x.Id);

        return topics.Select(x => new CommunityTopicDto
        {
            Id = x.Id,
            CategoryId = x.CategoryId,
            CategoryName = categories.TryGetValue(x.CategoryId, out var category) ? category.Name : "General",
            UserId = x.UserId,
            AuthorName = users.TryGetValue(x.UserId, out var user) ? user.NickName : "CodeMaster user",
            Title = x.Title,
            Content = x.Content,
            Status = x.Status,
            IsPinned = x.IsPinned,
            IsFeatured = x.IsFeatured,
            ViewCount = x.ViewCount,
            ReplyCount = x.ReplyCount,
            LikeCount = x.LikeCount,
            CreateTime = x.CreateTime
        }).ToList();
    }

    private long GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !long.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Login is required.");
        }

        return userId;
    }
}
