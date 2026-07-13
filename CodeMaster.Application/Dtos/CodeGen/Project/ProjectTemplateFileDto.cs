namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// Project template download result.
/// </summary>
public class ProjectTemplateFileDto
{
    /// <summary>
    /// Template ZIP file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Template ZIP file content.
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Project template upload result.
/// </summary>
public class ProjectTemplateUploadResultDto
{
    /// <summary>
    /// Saved template ZIP file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Saved template ZIP file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Upload time.
    /// </summary>
    public DateTime UploadedAt { get; set; }
}
