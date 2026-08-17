using AdminFlow.Budget.Domain.CostCenters;

namespace AdminFlow.Budget.UnitTests.CostCenters;

public sealed class CostCenterTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCostCenter()
    {
        var costCenter = new CostCenter("ADM-001", "Administration");

        Assert.NotEqual(Guid.Empty, costCenter.Id);
        Assert.Equal("ADM-001", costCenter.Code);
        Assert.Equal("Administration", costCenter.Name);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_ShouldTrimCodeAndName()
    {
        var costCenter = new CostCenter("  ADM-001  ", "  Administration  ");

        Assert.Equal("ADM-001", costCenter.Code);
        Assert.Equal("Administration", costCenter.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ShouldThrowArgumentException(string? invalidCode)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new CostCenter(invalidCode!, "Administration"));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new CostCenter("ADM-001", invalidName!));

        Assert.Equal("name", exception.ParamName);
    }
}
