using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 软删除接口
/// 实现此接口的实体将支持软删除功能，删除时不会真正从数据库删除，而是标记为已删除
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    DateTime? DeleteTime { get; set; }



    /// <summary>
    /// 删除人
    /// </summary>

    string? DeleteBy { get; set; }


    long? DeleteUserId { get; set; }
}
