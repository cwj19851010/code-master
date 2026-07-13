using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;

namespace CodeMaster.Infrastructure.ModelBinding;

/// <summary>
/// 查询字符串模型绑定器，支持从Query String绑定复杂对象
/// </summary>
public class QueryStringModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelType = bindingContext.ModelType;
        var instance = Activator.CreateInstance(modelType);

        if (instance == null)
        {
            return Task.CompletedTask;
        }

        var properties = modelType.GetProperties();
        foreach (var property in properties)
        {
            if (!property.CanWrite)
                continue;

            var valueProviderResult = bindingContext.ValueProvider.GetValue(property.Name);
            if (valueProviderResult == ValueProviderResult.None)
                continue;

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
                continue;

            try
            {
                var converter = TypeDescriptor.GetConverter(property.PropertyType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    var convertedValue = converter.ConvertFromString(value);
                    property.SetValue(instance, convertedValue);
                }
            }
            catch
            {
                // 忽略转换错误
            }
        }

        bindingContext.Result = ModelBindingResult.Success(instance);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 查询字��串模型绑定器提供器
/// </summary>
public class QueryStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // 只为复杂类型提供绑定器
        if (!context.Metadata.IsComplexType)
        {
            return null;
        }

        // 只有明确指定了 FromQuery 绑定源的参数才使用此绑定器
        // 这样可以避免干扰 FromBody 参数的绑定
        if (context.BindingInfo?.BindingSource != null &&
            context.BindingInfo.BindingSource.Id == "Query")
        {
            return new QueryStringModelBinder();
        }

        return null;
    }
}
