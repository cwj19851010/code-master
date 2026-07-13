namespace CodeMaster.Core.Entities;

/// <summary>
/// 实体标记接口（泛型），用于标识需要生成 EF Core 配置的实体
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IEntity<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    TKey Id { get; set; }
}

/// <summary>
/// 实体标记接口（非泛型），用于 Source Generator 识别
/// </summary>
public interface IEntity : IEntity<long>
{
}
