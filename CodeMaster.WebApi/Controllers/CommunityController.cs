using CodeMaster.Application.Dtos.Community;
using CodeMaster.Application.Services.Community;
using CodeMaster.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _communityService.GetCategoriesAsync();
        return Ok(ApiResponse<object>.Success(result, "OK"));
    }

    [AllowAnonymous]
    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] CommunityTopicQueryDto query)
    {
        var result = await _communityService.GetTopicsAsync(query);
        return Ok(ApiResponse<object>.Success(result, "OK"));
    }

    [AllowAnonymous]
    [HttpGet("topics/{id:long}")]
    public async Task<IActionResult> GetTopic(long id)
    {
        var result = await _communityService.GetTopicAsync(id);
        return result == null
            ? NotFound(ApiResponse<object>.Fail("Topic not found."))
            : Ok(ApiResponse<object>.Success(result, "OK"));
    }

    [Authorize]
    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateCommunityTopicDto dto)
    {
        var id = await _communityService.CreateTopicAsync(dto);
        return Ok(ApiResponse<object>.Success(new { id }, "Topic created."));
    }

    [AllowAnonymous]
    [HttpGet("topics/{topicId:long}/replies")]
    public async Task<IActionResult> GetReplies(long topicId)
    {
        var result = await _communityService.GetRepliesAsync(topicId);
        return Ok(ApiResponse<object>.Success(result, "OK"));
    }

    [Authorize]
    [HttpPost("replies")]
    public async Task<IActionResult> CreateReply([FromBody] CreateCommunityReplyDto dto)
    {
        var id = await _communityService.CreateReplyAsync(dto);
        return Ok(ApiResponse<object>.Success(new { id }, "Reply created."));
    }
}
