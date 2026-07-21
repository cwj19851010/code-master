using System.Diagnostics;
using System.Net.NetworkInformation;
using CodeMaster.Application.Dtos.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

public static class ProjectProcessLauncher
{
    public static Task<ProjectActionResultDto> StartFrontendAsync(string projectName, string projectPath, int? port)
    {
        return StartServiceAsync(
            serviceName: "前端服务",
            workingDirectory: Path.Combine(projectPath, $"{projectName}.Vue"),
            command: NpmCommand(),
            args: new[] { "run", "dev" },
            port: port,
            readyTimeout: TimeSpan.FromSeconds(10));
    }

    public static Task<ProjectActionResultDto> StartBackendAsync(string projectName, string projectPath, int? port)
    {
        var args = new List<string> { "run" };
        if (port is > 0)
        {
            // Do not rely on launchSettings.json here. MCP/UI starts the child process
            // outside Visual Studio, so an explicit URL prevents it from silently
            // falling back to ASP.NET Core's default port (usually 5000).
            args.Add("--no-launch-profile");
            args.Add("--");
            args.Add("--urls");
            args.Add($"http://localhost:{port.Value}");
            args.Add("--environment");
            args.Add("Development");
        }

        return StartServiceAsync(
            serviceName: "后端服务",
            workingDirectory: Path.Combine(projectPath, $"{projectName}.WebApi"),
            command: "dotnet",
            args: args,
            port: port,
            readyTimeout: TimeSpan.FromSeconds(90),
            unsetEnvironmentVariables: new[] { "ConnectionStrings__DefaultConnection", "DbProvider" });
    }

    public static async Task<ProjectActionResultDto> StartServiceAsync(
        string serviceName,
        string workingDirectory,
        string command,
        IReadOnlyList<string> args,
        int? port,
        TimeSpan readyTimeout,
        IReadOnlyList<string>? unsetEnvironmentVariables = null)
    {
        if (!Directory.Exists(workingDirectory))
        {
            return new ProjectActionResultDto
            {
                Success = false,
                Message = $"{serviceName}目录不存在: {workingDirectory}"
            };
        }

        var url = BuildLocalUrl(port);
        var restartedFromExistingProcess = false;
        if (TryGetListeningProcessIdByPort(port) is { } existingPid)
        {
            if (!TryKillProcess(existingPid, out var killError))
            {
                return new ProjectActionResultDto
                {
                    Success = false,
                    Message = $"{serviceName}端口已被 PID {existingPid} 占用，且停止失败: {killError}",
                    Output = $"WorkingDirectory: {workingDirectory}"
                };
            }

            restartedFromExistingProcess = true;
            await WaitForPortReleaseAsync(port!.Value, TimeSpan.FromSeconds(5));
        }

        var process = StartVisibleTerminal(command, args, workingDirectory, unsetEnvironmentVariables);
        if (!port.HasValue || port.Value <= 0)
        {
            return BuildResult(serviceName, restartedFromExistingProcess, url, process?.Id, workingDirectory);
        }

        var ready = await WaitForPortAsync(port.Value, readyTimeout);
        if (ready)
        {
            var pid = TryGetListeningProcessIdByPort(port) ?? process?.Id;
            return BuildResult(serviceName, restartedFromExistingProcess, url, pid, workingDirectory);
        }

        return new ProjectActionResultDto
        {
            Success = false,
            Message = $"{serviceName}启动命令已执行，但端口 {port.Value} 在 {readyTimeout.TotalSeconds:0} 秒内未监听",
            Output = $"WorkingDirectory: {workingDirectory}"
        };
    }

    public static async Task<bool> WaitForPortAsync(int port, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            if (IsPortListening(port))
                return true;

            await Task.Delay(500);
        }

        return IsPortListening(port);
    }

    public static async Task<bool> WaitForPortReleaseAsync(int port, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            if (!IsPortListening(port))
                return true;

            await Task.Delay(300);
        }

        return !IsPortListening(port);
    }

    public static bool IsPortListening(int? port)
    {
        if (!port.HasValue || port.Value <= 0)
            return false;

        return IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Any(endpoint => endpoint.Port == port.Value);
    }

    public static int? TryGetListeningProcessIdByPort(int? port)
    {
        if (!port.HasValue || port.Value <= 0 || !OperatingSystem.IsWindows())
            return null;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-ano");

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            foreach (var line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5 || !parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!EndpointHasPort(parts[1], port.Value))
                    continue;

                if (int.TryParse(parts[^1], out var pid))
                    return pid;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public static string NpmCommand()
    {
        return OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
    }

    private static Process? StartVisibleTerminal(
        string command,
        IReadOnlyList<string> args,
        string workingDirectory,
        IReadOnlyList<string>? unsetEnvironmentVariables)
    {
        if (OperatingSystem.IsWindows())
        {
            var unsetPrefix = unsetEnvironmentVariables is { Count: > 0 }
                ? string.Join(" && ", unsetEnvironmentVariables.Select(name => $"set \"{name}=\"")) + " && "
                : string.Empty;
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {unsetPrefix}{BuildCommandLine(command, args)}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                CreateNoWindow = false
            });
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);
        if (unsetEnvironmentVariables != null)
        {
            foreach (var name in unsetEnvironmentVariables)
                startInfo.Environment.Remove(name);
        }

        return Process.Start(startInfo);
    }

    private static string BuildCommandLine(string command, IReadOnlyList<string> args)
    {
        return string.Join(' ', new[] { QuoteForCmd(command) }.Concat(args.Select(QuoteForCmd)));
    }

    private static string QuoteForCmd(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "\"\"";

        return value.Any(char.IsWhiteSpace) || value.Contains('&') || value.Contains('(') || value.Contains(')')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }

    private static ProjectActionResultDto BuildResult(string serviceName, bool restartedFromExistingProcess, string url, int? pid, string workingDirectory)
    {
        var stateText = restartedFromExistingProcess ? "已重启" : "已启动";
        var pidText = pid.HasValue ? $"PID {pid.Value}" : "PID 未知";
        var message = string.IsNullOrWhiteSpace(url)
            ? $"{serviceName}{stateText}，已打开 CMD 窗口（{pidText}）"
            : $"{serviceName}{stateText}：{url}，已打开 CMD 窗口（{pidText}）";

        return new ProjectActionResultDto
        {
            Success = true,
            Message = message,
            Output = $"WorkingDirectory: {workingDirectory}"
        };
    }

    private static string BuildLocalUrl(int? port)
    {
        return port.HasValue && port.Value > 0 ? $"http://localhost:{port.Value}" : string.Empty;
    }

    private static bool EndpointHasPort(string endpoint, int port)
    {
        return endpoint.EndsWith($":{port}", StringComparison.Ordinal) ||
            endpoint.EndsWith($"]:{port}", StringComparison.Ordinal);
    }

    private static bool TryKillProcess(int processId, out string? error)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
