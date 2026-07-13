namespace CodeMaster.Core.Exceptions;

/// <summary>
/// 未找到异常
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(404, message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base(404, $"{entityName} with id {key} was not found.")
    {
    }
}
