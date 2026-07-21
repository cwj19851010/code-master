using CodeMaster.Application.Services.CodeGen;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class BuildDiagnosticFormatterTests
{
    [Fact]
    public void Summarize_PrioritizesCompilerErrorsAfterLongWarningOutput()
    {
        var warnings = string.Join('\n', Enumerable.Range(1, 500)
            .Select(index => $"Project.csproj : warning NU1903: warning {index}"));
        var error = @"D:\Generated\Entity.auto.cs(10,44): error CS0535: Entity does not implement IEntity<long>.Id";
        var output = $"{warnings}\n{error}\nBuild FAILED.\n1 Error(s)";

        var summary = BuildDiagnosticFormatter.Summarize(output, "dotnet build exited with code 1", 2000);

        Assert.Contains("error CS0535", summary);
        Assert.Contains("Entity.auto.cs(10,44)", summary);
        Assert.Contains("exited with code 1", summary);
        Assert.True(summary.Length <= 2000);
    }

    [Fact]
    public void Summarize_AddsMetadataHintForMissingEntityMember()
    {
        const string output = @"D:\Generated\OrderService.cs(42,18): error CS1061: 'Order' does not contain a definition for 'CustomerName'";

        var summary = BuildDiagnosticFormatter.Summarize(output);

        Assert.Contains("CodeMaster metadata checks", summary);
        Assert.Contains("Order.CustomerName", summary);
        Assert.Contains("entity field metadata", summary);
    }
}
