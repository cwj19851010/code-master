namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 语言DTO
/// </summary>
public class SysLangDto
{
    public long Id { get; set; }
    public string LangCode { get; set; } = string.Empty;
    public string LangName { get; set; } = string.Empty;
    public int IsDefault { get; set; }
    public int IsEnabled { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
}

public class CreateSysLangDto
{
    public string LangCode { get; set; } = string.Empty;
    public string LangName { get; set; } = string.Empty;
    public int IsDefault { get; set; } = 0;
    public int IsEnabled { get; set; } = 1;
    public int Sort { get; set; } = 0;
    public string? Remark { get; set; }
}

public class UpdateSysLangDto
{
    public long Id { get; set; }
    public string? LangCode { get; set; }
    public string? LangName { get; set; }
    public int? IsDefault { get; set; }
    public int? IsEnabled { get; set; }
    public int? Sort { get; set; }
    public string? Remark { get; set; }
}

public class SysLangQueryDto
{
    public string? LangCode { get; set; }
    public string? LangName { get; set; }
    public int? IsEnabled { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 语言文本DTO
/// </summary>
public class SysLangTextDto
{
    public long Id { get; set; }
    public string LangCode { get; set; } = string.Empty;
    public string LangKey { get; set; } = string.Empty;
    public string LangValue { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
}

public class CreateSysLangTextDto
{
    public string LangCode { get; set; } = string.Empty;
    public string LangKey { get; set; } = string.Empty;
    public string LangValue { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Remark { get; set; }
}

public class UpdateSysLangTextDto
{
    public long Id { get; set; }
    public string? LangCode { get; set; }
    public string? LangKey { get; set; }
    public string? LangValue { get; set; }
    public string? Category { get; set; }
    public string? Remark { get; set; }
}

public class SysLangTextQueryDto
{
    public string? LangCode { get; set; }
    public string? LangKey { get; set; }
    public string? Category { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
