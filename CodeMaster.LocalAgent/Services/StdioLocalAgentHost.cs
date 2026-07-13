using System.Text.Json;
using CodeMaster.LocalAgent.Models;

namespace CodeMaster.LocalAgent.Services;

public class StdioLocalAgentHost
{
    private readonly LocalCodegenExecutionService _executor;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public StdioLocalAgentHost(LocalCodegenExecutionService executor)
    {
        _executor = executor;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var stdin = Console.In;
        var protocolWriter = Console.Out;

        // stdout is reserved for newline-delimited JSON protocol messages.
        Console.SetOut(TextWriter.Null);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await stdin.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

            line = line.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var response = await ExecuteLineAsync(line);
            await protocolWriter.WriteLineAsync(JsonSerializer.Serialize(response, _jsonOptions));
            await protocolWriter.FlushAsync();
        }
    }

    private async Task<StdioLocalExecutionResponse> ExecuteLineAsync(string line)
    {
        string requestId = string.Empty;

        try
        {
            var request = JsonSerializer.Deserialize<StdioLocalExecutionRequest>(line, _jsonOptions)
                ?? throw new InvalidOperationException("Empty stdio request");

            requestId = request.Id;
            var result = await _executor.ExecuteAsync(request.Action, request);

            return new StdioLocalExecutionResponse
            {
                Id = requestId,
                Success = result.Success,
                Message = result.Message,
                Data = result.Data,
                Output = result.Output,
                DataOnly = result.DataOnly
            };
        }
        catch (Exception ex)
        {
            return new StdioLocalExecutionResponse
            {
                Id = requestId,
                Success = false,
                Message = FormatExceptionMessage(ex),
                Output = ex.ToString()
            };
        }
    }

    private static string FormatExceptionMessage(Exception ex)
    {
        var baseException = ex.GetBaseException();
        var message = baseException.Message;

        if (string.IsNullOrWhiteSpace(message))
            message = ex.Message;

        if (string.IsNullOrWhiteSpace(message))
            message = baseException.GetType().FullName ?? ex.GetType().FullName ?? "Unknown local execution error";

        return message;
    }
}
