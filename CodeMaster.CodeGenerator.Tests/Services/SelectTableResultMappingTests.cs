using CodeMaster.Application.Services.CodeGen.Relations;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class SelectTableResultMappingTests
{
    [Fact]
    public void Parse_EmptyMetadata_PreservesLegacyBehavior()
    {
        Assert.Empty(SelectTableResultMappingParser.Parse(null));
        Assert.Empty(SelectTableResultMappingParser.Parse(string.Empty));
        Assert.Empty(SelectTableResultMappingParser.Parse("[]"));
    }

    [Fact]
    public void Parse_ValidMappings_ReturnsEveryAssignment()
    {
        var mappings = SelectTableResultMappingParser.Parse(
            "[{\"sourceField\":\"Id\",\"targetField\":\"ProductId\"},{\"sourceField\":\"Name\",\"targetField\":\"ProductName\"}]");

        Assert.Collection(
            mappings,
            mapping =>
            {
                Assert.Equal("Id", mapping.SourceField);
                Assert.Equal("ProductId", mapping.TargetField);
            },
            mapping =>
            {
                Assert.Equal("Name", mapping.SourceField);
                Assert.Equal("ProductName", mapping.TargetField);
            });
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[{\"sourceField\":\"Name\"}]")]
    [InlineData("[{\"targetField\":\"ProductName\"}]")]
    public void Parse_InvalidMappings_ThrowsWithoutSilentlyDroppingRows(string json)
    {
        Assert.Throws<InvalidOperationException>(() => SelectTableResultMappingParser.Parse(json));
    }
}
