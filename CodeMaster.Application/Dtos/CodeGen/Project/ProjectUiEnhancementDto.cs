namespace CodeMaster.Application.Dtos.CodeGen;

public sealed class ProjectUiEnhancementDto
{
    public long ProjectId { get; set; }

    public string TargetKind { get; set; } = "Scaffold";

    public string Page { get; set; } = "Login";

    public long? EntityId { get; set; }

    public string? PageType { get; set; }

    public string Style { get; set; } = "Enterprise";

    public string? Headline { get; set; }

    public string? Subtitle { get; set; }

    public List<string> Highlights { get; set; } = new();

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public bool ReplaceExistingDesign { get; set; }

    public List<ProjectUiNodeOperationDto> Operations { get; set; } = new();
}

public sealed class ProjectUiNodeOperationDto
{
    public string OperationId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? TargetGenId { get; set; }

    public string? TargetNodeId { get; set; }

    public string TargetScope { get; set; } = "Self";

    public string? Tag { get; set; }

    public string? ParentGenId { get; set; }

    public string ParentScope { get; set; } = "Self";

    public string? BeforeGenId { get; set; }

    public string? AfterGenId { get; set; }

    public string? PropName { get; set; }

    public string? PropValue { get; set; }

    public bool IsBind { get; set; }

    public bool IsSingle { get; set; }

    public int? Xs { get; set; }

    public int? Sm { get; set; }

    public int? Md { get; set; }

    public int? Lg { get; set; }

    public string? GroupId { get; set; }

    public string? GroupTitle { get; set; }

    public List<string> MemberGenIds { get; set; } = new();
}

public sealed class ProjectUiDesignDocumentDto
{
    public int Version { get; set; } = 1;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ProjectUiNodeOperationDto> Operations { get; set; } = new();
}

public sealed class ProjectUiEnhancementResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Page { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? BackupPath { get; set; }

    public string? DesignPath { get; set; }

    public int AppliedOperationCount { get; set; }
}
