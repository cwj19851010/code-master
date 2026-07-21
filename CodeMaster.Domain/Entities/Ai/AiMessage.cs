using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Ai;

/// <summary>
/// Persisted conversation message.
/// </summary>
[SugarTable("sys_ai_message")]
public class AiMessage : EntityBaseWithTenant
{
    [SugarColumn(ColumnName = "conversation_id", IsNullable = false)]
    public long ConversationId { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "request_id", Length = 64, IsNullable = true)]
    public string? RequestId { get; set; }

    [SugarColumn(ColumnName = "role", Length = 20, IsNullable = false)]
    public string Role { get; set; } = "user";

    [SugarColumn(ColumnName = "content", Length = 8000, IsNullable = false)]
    public string Content { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "metadata_json", Length = 4000, IsNullable = true)]
    public string? MetadataJson { get; set; }

    [SugarColumn(ColumnName = "sequence", IsNullable = false)]
    public int Sequence { get; set; }
}
