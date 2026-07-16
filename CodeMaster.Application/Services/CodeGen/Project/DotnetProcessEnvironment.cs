using System.Diagnostics;

namespace CodeMaster.Application.Services.CodeGen;

public static class DotnetProcessEnvironment
{
    public static void Apply(ProcessStartInfo startInfo)
    {
        if (!IsDotnetCommand(startInfo.FileName))
            return;

        var home = GetWritableDotnetHome();
        startInfo.Environment["DOTNET_CLI_HOME"] = home;
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        if (!startInfo.Environment.ContainsKey("NUGET_PACKAGES") ||
            !IsWritableDirectory(startInfo.Environment["NUGET_PACKAGES"]))
        {
            var packages = Path.Combine(home, ".nuget", "packages");
            Directory.CreateDirectory(packages);
            startInfo.Environment["NUGET_PACKAGES"] = packages;
        }
    }

    private static bool IsDotnetCommand(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var name = Path.GetFileNameWithoutExtension(fileName);
        return string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetWritableDotnetHome()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("DOTNET_CLI_HOME"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.Combine(Path.GetTempPath(), "codemaster", "dotnet-home")
        };

        foreach (var candidate in candidates)
        {
            if (IsWritableDirectory(candidate))
                return Path.GetFullPath(candidate!);
        }

        var fallback = Path.Combine(Path.GetTempPath(), "codemaster", "dotnet-home");
        Directory.CreateDirectory(fallback);
        return Path.GetFullPath(fallback);
    }

    private static bool IsWritableDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".codemaster-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
