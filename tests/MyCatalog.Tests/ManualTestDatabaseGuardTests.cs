using MyCatalog.Data;

namespace MyCatalog.Tests;

public class ManualTestDatabaseGuardTests
{
    [Fact]
    public void AcceptsExpectedIsolatedLocalDbWithoutOpeningConnection()
    {
        var result = ManualTestDatabaseGuard.Validate(
            @"Server=(localdb)\MSSQLLocalDB;Database=MyCatalogManualTest_BookCovers;Trusted_Connection=True",
            "MyCatalogManualTest_BookCovers");
        Assert.Equal("MyCatalogManualTest_BookCovers", result.Database);
    }

    [Theory]
    [InlineData(@"Server=(localdb)\MSSQLLocalDB;Database=MyCatalogCompletoDb", "MyCatalogManualTest_BookCovers")]
    [InlineData(@"Server=(localdb)\MSSQLLocalDB;Database=OtherDatabase", "MyCatalogManualTest_BookCovers")]
    [InlineData(@"Server=OtherServer;Database=MyCatalogManualTest_BookCovers", "MyCatalogManualTest_BookCovers")]
    [InlineData(@"Server=(localdb)\MSSQLLocalDB;Database=MyCatalogCompletoDb", "MyCatalogCompletoDb")]
    [InlineData("", "MyCatalogManualTest_BookCovers")]
    public void RejectsWrongTargetBeforeAnyConnection(string connection, string expected)
    {
        Assert.Throws<InvalidOperationException>(() => ManualTestDatabaseGuard.Validate(connection, expected));
    }
}
