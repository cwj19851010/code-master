namespace CodeMaster.Application.Dtos.Auth;

public class SendEmailCodeDto
{
    public string Email { get; set; } = string.Empty;

    public string Purpose { get; set; } = "register";
}

public class RegisterWithEmailDto
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? NickName { get; set; }
}

public class GithubAuthorizeUrlDto
{
    public string AuthorizeUrl { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}

public class GithubCallbackDto
{
    public string Code { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}
