using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Core.Attributes;
using CodeMaster.Core.Enums;
using CodeMaster.Domain.Entities.Monitor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeMaster.WebApi.Filters;

/// <summary>
/// Global operation log filter.
/// Explicit [Log] methods are always recorded. Authenticated API write operations are recorded automatically.
/// </summary>
public class GlobalOperationLogFilter : IAsyncActionFilter
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "passwd",
        "secret",
        "apikey",
        "token",
        "authorization",
        "connectionstring"
    ];

    private readonly ISysOperLogService _operLogService;
    private readonly ILogger<GlobalOperationLogFilter> _logger;

    public GlobalOperationLogFilter(ISysOperLogService operLogService, ILogger<GlobalOperationLogFilter> logger)
    {
        _operLogService = operLogService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var logAttribute = actionDescriptor?.MethodInfo.GetCustomAttributes(typeof(LogAttribute), false)
            .FirstOrDefault() as LogAttribute;

        if (logAttribute == null && !ShouldAutoLog(context))
        {
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var operLog = new SysOperLog
        {
            Title = GetLogTitle(actionDescriptor, logAttribute),
            BusinessType = (int)(logAttribute?.BusinessType ?? InferBusinessType(context, actionDescriptor)),
            Method = $"{actionDescriptor?.ControllerName}.{actionDescriptor?.ActionName}",
            RequestMethod = context.HttpContext.Request.Method,
            OperatorType = (int)(logAttribute?.OperatorType ?? OperatorType.Manage),
            OperName = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous",
            OperUrl = context.HttpContext.Request.Path.Value ?? string.Empty,
            OperIp = GetClientIp(context.HttpContext),
            OperTime = DateTime.UtcNow,
            Status = 0
        };

        if (logAttribute?.IsSaveRequestData ?? true)
        {
            operLog.OperParam = GetRequestParams(context);
        }

        try
        {
            var executedContext = await next();
            stopwatch.Stop();
            operLog.Elapsed = stopwatch.ElapsedMilliseconds;

            if ((logAttribute?.IsSaveResponseData ?? false) && executedContext.Result is ObjectResult objectResult)
            {
                operLog.JsonResult = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                if (operLog.JsonResult?.Length > 4000)
                {
                    operLog.JsonResult = operLog.JsonResult[..4000] + "...";
                }
            }

            var statusCode = GetResultStatusCode(executedContext.Result) ?? context.HttpContext.Response.StatusCode;
            operLog.Status = executedContext.Exception == null && statusCode < 400 ? 0 : 1;
            if (executedContext.Exception != null)
            {
                operLog.ErrorMsg = Truncate(executedContext.Exception.Message, 4000);
            }
            else if (statusCode >= 400)
            {
                operLog.ErrorMsg = $"HTTP {statusCode}";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            operLog.Elapsed = stopwatch.ElapsedMilliseconds;
            operLog.Status = 1;
            operLog.ErrorMsg = Truncate(ex.Message, 4000);
            throw;
        }
        finally
        {
            try
            {
                await _operLogService.InsertOperLogAsync(operLog);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write operation log for {Method} {Path}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path.Value);
            }
        }
    }

    private static bool ShouldAutoLog(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (!request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        if (request.Path.StartsWithSegments("/api/auth") ||
            request.Path.StartsWithSegments("/api/account") ||
            request.Path.StartsWithSegments("/api/monitor/operlog") ||
            request.Path.StartsWithSegments("/api/monitor/loginlog"))
        {
            return false;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return request.Method is not ("GET" or "HEAD" or "OPTIONS");
    }

    private static string GetLogTitle(ControllerActionDescriptor? actionDescriptor, LogAttribute? logAttribute)
    {
        if (!string.IsNullOrWhiteSpace(logAttribute?.Title))
        {
            return Truncate(logAttribute.Title, 50);
        }

        var title = $"{actionDescriptor?.ControllerName ?? "Unknown"}.{actionDescriptor?.ActionName ?? "Unknown"}";
        return Truncate(title, 50);
    }

    private static BusinessType InferBusinessType(ActionExecutingContext context, ControllerActionDescriptor? actionDescriptor)
    {
        var actionName = actionDescriptor?.ActionName ?? string.Empty;

        if (actionName.Contains("Clear", StringComparison.OrdinalIgnoreCase))
            return BusinessType.Clean;
        if (actionName.Contains("Export", StringComparison.OrdinalIgnoreCase))
            return BusinessType.Export;
        if (actionName.Contains("Import", StringComparison.OrdinalIgnoreCase))
            return BusinessType.Import;
        if (actionName.Contains("Force", StringComparison.OrdinalIgnoreCase))
            return BusinessType.Force;

        return context.HttpContext.Request.Method.ToUpperInvariant() switch
        {
            "POST" when actionName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
                     || actionName.StartsWith("Add", StringComparison.OrdinalIgnoreCase)
                     || actionName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase) => BusinessType.Insert,
            "PUT" or "PATCH" => BusinessType.Update,
            "DELETE" => BusinessType.Delete,
            _ => BusinessType.Other
        };
    }

    private static string GetClientIp(HttpContext context)
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

    private static int? GetResultStatusCode(IActionResult? result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };
    }

    private static string GetRequestParams(ActionExecutingContext context)
    {
        try
        {
            var parameters = new Dictionary<string, object?>();

            foreach (var param in context.ActionArguments)
            {
                parameters[param.Key] = param.Value;
            }

            foreach (var query in context.HttpContext.Request.Query)
            {
                parameters[query.Key] = query.Value.ToString();
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var node = JsonSerializer.SerializeToNode(parameters, options);
            RedactSensitiveValues(node);
            var json = node?.ToJsonString(options) ?? string.Empty;

            return Truncate(json, 4000);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void RedactSensitiveValues(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitiveKey(property.Key))
                {
                    jsonObject[property.Key] = "***";
                }
                else
                {
                    RedactSensitiveValues(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                RedactSensitiveValues(item);
            }
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty).Replace("-", string.Empty);
        return SensitiveKeyFragments.Any(fragment =>
            normalized.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
