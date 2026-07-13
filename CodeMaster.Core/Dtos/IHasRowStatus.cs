namespace CodeMaster.Core.Dtos;

/// <summary>
/// 具有行状态的 DTO（Update 子表 DTO 实现此接口）
/// </summary>
public interface IHasRowStatus
{
    RowStatus RowStatus { get; set; }
}
