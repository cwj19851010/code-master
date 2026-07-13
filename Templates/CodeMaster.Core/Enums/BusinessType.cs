namespace CodeMaster.Core.Enums;

/// <summary>
/// 业务操作类型
/// </summary>
public enum BusinessType
{
    /// <summary>
    /// 其它
    /// </summary>
    Other = 0,

    /// <summary>
    /// 新增
    /// </summary>
    Insert = 1,

    /// <summary>
    /// 修改
    /// </summary>
    Update = 2,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 授权
    /// </summary>
    Grant = 4,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 强退
    /// </summary>
    Force = 7,

    /// <summary>
    /// 清空数据
    /// </summary>
    Clean = 8,

    /// <summary>
    /// 查询
    /// </summary>
    Query = 9
}
