namespace CodeMaster.Application.Dtos.CodeGen;

public class ProjectCodeGenerationDto
{
    public long ProjectId { get; set; }

    public List<long> EntityIds { get; set; } = new();
}
