using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Core.Attributes;
using CodeMaster.Domain.Entities.Monitor;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeMaster.WebApi.Filters;

/// <summary>
/// 全局操作日志过滤器
/// </summary>
public class GlobalOperationLogFilter : IAsyncActionFilter
{
    private readonly ISysOperLogService _operLogService;

    public GlobalOperationLogFilter(ISysOperLogService operLogService)
    {
        _operLogService = operLogService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 检查是否标记了 [Log] 特性
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var logAttribute = actionDescriptor?.MethodInfo.GetCustomAttributes(typeof(LogAttribute), false)
            .FirstOrDefault() as LogAttribute;

        if (logAttribute == null)
        {
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var operLog = new SysOperLog
        {
            Title = logAttribute.Title,
            BusinessType = (int)logAttribute.BusinessType,
            Method = $"{actionDescriptor?.ControllerName}.{actionDescriptor?.ActionName}",
            RequestMethod = context.HttpContext.Request.Method,
            OperatorType = (int)logAttribute.OperatorType,
            OperName = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous",
            OperUrl = context.HttpContext.Request.Path,
            OperIp = GetClientIp(context.HttpContext),
            OperTime = DateTime.UtcNow,
            Status = 0
        };

        // 保存请求参数
        if (logAttribute.IsSaveRequestData)
        {
            operLog.OperParam = GetRequestParams(context);
        }

        ActionExecutedContext? executedContext = null;
        try
        {
            executedContext = await next();
            stopwatch.Stop();
            operLog.Elapsed = stopwatch.ElapsedMilliseconds;

            // 保存响应数据
            if (logAttribute.IsSaveResponseData && executedContext.Result is ObjectResult objectResult)
            {
                operLog.JsonResult = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // 限制响应数据长度
                if (operLog.JsonResult?.Length > 4000)
                {
                    operLog.JsonResult = operLog.JsonResult.Substring(0, 4000) + "...";
                }
            }

            operLog.Status = executedContext.Exception == null ? 0 : 1;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            operLog.Elapsed = stopwatch.ElapsedMilliseconds;
            operLog.Status = 1;
            operLog.ErrorMsg = ex.Message;

            if (operLog.ErrorMsg?.Length > 4000)
            {
                operLog.ErrorMsg = operLog.ErrorMsg.Substring(0, 4000);
            }

            throw;
        }
        finally
        {
            // 异步写入日志，不阻塞主流程
            _ = Task.Run(async () =>
            {
                try
                {
                    await _operLogService.InsertOperLogAsync(operLog);
                }
                catch
                {
                    // 忽略日志写入失败
                }
            });
        }
    }

    /// <summary>
    /// 获取客户端IP
    /// </summary>
    private string GetClientIp(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Connection.RemoteIpAddress?.ToString();
        }
        return ip ?? "Unknown";
    }

    /// <summary>
    /// 获取请求参数
    /// </summary>
    private string GetRequestParams(ActionExecutingContext context)
    {
        try
        {
            var parameters = new Dictionary<string, object?>();

            // 获取路由参数
            foreach (var param in context.ActionArguments)
            {
                parameters[param.Key] = param.Value;
            }

            // 获取查询字符串
            foreach (var query in context.HttpContext.Request.Query)
            {
                parameters[query.Key] = query.Value.ToString();
            }

            var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // 限制参数长度
            if (json.Length > 4000)
            {
                json = json.Substring(0, 4000) + "...";
            }

            return json;
        }
        catch
        {
            return string.Empty;
        }
    }
}
