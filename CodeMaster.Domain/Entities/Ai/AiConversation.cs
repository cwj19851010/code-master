using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Ai;

/// <summary>
/// Project-bound AI conversation.
/// </summary>
[SugarTable("sys_ai_conversation")]
public class AiConversation : EntityBaseWithTenant
{
    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "provider_id", IsNullable = false)]
    public long ProviderId { get; set; }

    [SugarColumn(ColumnName = "title", Length = 200, IsNullable = false)]
    public string Title { get; set; } = "New conversation";

    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "Active";

    [SugarColumn(ColumnName = "context_summary", Length = 8000, IsNullable = true)]
    public string? ContextSummary { get; set; }

    [SugarColumn(ColumnName = "session_json", Length = 8000, IsNullable = true)]
    public string? SessionJson { get; set; }

    [SugarColumn(ColumnName = "last_message_at", IsNullable = true)]
    public DateTime? LastMessageAt { get; set; }
}
