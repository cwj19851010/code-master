using Microsoft.AspNetCore.DataProtection;

namespace CodeMaster.Agent.Services;

public interface IAiSecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}

internal sealed class AiSecretProtector : IAiSecretProtector
{
    private readonly IDataProtector _protector;

    public AiSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("CodeMaster.Agent.ProviderSecrets.v1");
    }

    public string Protect(string value) => _protector.Protect(value);

    public string Unprotect(string value) => _protector.Unprotect(value);
}
