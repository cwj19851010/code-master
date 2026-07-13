using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace CodeMaster.Infrastructure.Middleware;

/// <summary>
/// HTTP请求日志中间件
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 记录请求开始时间
        var stopwatch = Stopwatch.StartNew();

        // 记录请求信息
        var request = context.Request;
        var requestLog = new StringBuilder();
        requestLog.AppendLine($"[HTTP Request] {request.Method} {request.Path}{request.QueryString}");
        requestLog.AppendLine($"  Host: {request.Host}");
        requestLog.AppendLine($"  ContentType: {request.ContentType}");
        requestLog.AppendLine($"  UserAgent: {request.Headers["User-Agent"]}");

        // 记录请求头（排除敏感信息）
        if (request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                requestLog.AppendLine($"  Authorization: Bearer ***");
            }
        }

        _logger.LogInformation(requestLog.ToString());

        // 保存原始响应流
        var originalBodyStream = context.Response.Body;

        try
        {
            // 执行下一个中间件
            await _next(context);

            // 记录响应信息
            stopwatch.Stop();
            var responseLog = new StringBuilder();
            responseLog.AppendLine($"[HTTP Response] {request.Method} {request.Path} - {context.Response.StatusCode}");
            responseLog.AppendLine($"  Duration: {stopwatch.ElapsedMilliseconds}ms");
            responseLog.AppendLine($"  ContentType: {context.Response.ContentType}");

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(responseLog.ToString() + "  [SLOW REQUEST]");
            }
            else
            {
                _logger.LogInformation(responseLog.ToString());
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, $"[HTTP Error] {request.Method} {request.Path} - Duration: {stopwatch.ElapsedMilliseconds}ms");
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>
/// 请求日志中间件扩展
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
