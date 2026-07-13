namespace CodeMaster.Core.Exceptions;

/// <summary>
/// 业务异常
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// 错误码
    /// </summary>
    public int Code { get; set; }

    public BusinessException(string message) : base(message)
    {
        Code = 500;
    }

    public BusinessException(int code, string message) : base(message)
    {
        Code = code;
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
        Code = 500;
    }
}
