using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CodeMaster.WebApi.Filters;

/// <summary>
/// 动态API操作过滤器，确保Swagger正确识别HTTP Method
/// </summary>
public class DynamicApiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 确保每个操作都有明确的 OperationId
        if (string.IsNullOrEmpty(operation.OperationId))
        {
            var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
            operation.OperationId = $"{controllerName}_{actionName}";
        }
    }
}
