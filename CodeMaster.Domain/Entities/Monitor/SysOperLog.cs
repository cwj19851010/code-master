using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Monitor;

/// <summary>
/// 操作日志表
/// </summary>
[SugarTable("sys_oper_log")]
public class SysOperLog : EntityBase
{
    /// <summary>
    /// 模块标题
    /// </summary>
    [SugarColumn(ColumnName = "title", Length = 50)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务类型（0其它 1新增 2修改 3删除 4授权 5导出 6导入 7强退 8清空）
    /// </summary>
    [SugarColumn(ColumnName = "business_type")]
    public int BusinessType { get; set; }

    /// <summary>
    /// 方法名称
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 200)]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求方式
    /// </summary>
    [SugarColumn(ColumnName = "request_method", Length = 10)]
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// 操作类别（0其它 1后台用户 2手机端用户）
    /// </summary>
    [SugarColumn(ColumnName = "operator_type")]
    public int OperatorType { get; set; }

    /// <summary>
    /// 操作人员
    /// </summary>
    [SugarColumn(ColumnName = "oper_name", Length = 50)]
    public string OperName { get; set; } = string.Empty;

    /// <summary>
    /// 请求URL
    /// </summary>
    [SugarColumn(ColumnName = "oper_url", Length = 500)]
    public string OperUrl { get; set; } = string.Empty;

    /// <summary>
    /// 主机地址
    /// </summary>
    [SugarColumn(ColumnName = "oper_ip", Length = 50)]
    public string OperIp { get; set; } = string.Empty;

    /// <summary>
    /// 操作地点
    /// </summary>
    [SugarColumn(ColumnName = "oper_location", Length = 255, IsNullable = true)]
    public string? OperLocation { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    [SugarColumn(ColumnName = "oper_param", Length = 4000, IsNullable = true)]
    public string? OperParam { get; set; }

    /// <summary>
    /// 返回参数
    /// </summary>
    [SugarColumn(ColumnName = "json_result", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? JsonResult { get; set; }

    /// <summary>
    /// 操作状态（0成功 1失败）
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnName = "error_msg", Length = 4000, IsNullable = true)]
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(ColumnName = "oper_time")]
    public DateTime OperTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "elapsed")]
    public long Elapsed { get; set; }
}
