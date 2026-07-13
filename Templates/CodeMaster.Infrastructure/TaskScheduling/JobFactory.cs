using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace CodeMaster.Infrastructure.TaskScheduling;

/// <summary>
/// Job工厂，支持依赖注入
/// </summary>
public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建Job实例
    /// </summary>
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        try
        {
            var serviceScope = _serviceProvider.CreateScope();
            var job = serviceScope.ServiceProvider.GetService(bundle.JobDetail.JobType) as IJob;
            return job ?? throw new Exception($"无法创建Job实例: {bundle.JobDetail.JobType.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建Job失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 释放Job实例
    /// </summary>
    public void ReturnJob(IJob job)
    {
        if (job is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
