namespace CodeMaster.Core.Entities;

/// <summary>
/// 实体顶级标记接口
/// 只要实现了该接口的类，都会被 SourceGenerator 扫描并由 EF Core 进行数据库映射：
/// - 有主键字段 → 生成标准物理表
/// - 无主键字段 → 生成 builder.HasNoKey() 无主键映射
/// </summary>
public interface IBaseEntity
{
}
