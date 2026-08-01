using Waller.Native.Workflows.Storage;

namespace Waller.Native.Tests.Workflows;

public sealed class LocalDataLayoutTests
{
    [Fact]
    public void Create_ResolvesPackagedRootsDeterministically()
    {
        var layout = LocalDataLayout.Create(
            @"C:\Users\Casey\AppData\Local\Packages\Waller_123\LocalCache\Local",
            @"C:\Users\Casey");

        Assert.Equal(
            @"C:\Users\Casey\AppData\Local\Packages\Waller_123\LocalCache\Local\Waller",
            layout.AppDataRoot);
        Assert.Equal(@"C:\Users\Casey\.waller", layout.RenderedCacheRoot);
    }

    [Fact]
    public void Create_ResolvesUnpackagedRootsDeterministically()
    {
        var layout = LocalDataLayout.Create(
            @"D:\Users\Riley\AppData\Local",
            @"D:\Users\Riley");

        Assert.Equal(@"D:\Users\Riley\AppData\Local\Waller", layout.AppDataRoot);
        Assert.Equal(@"D:\Users\Riley\.waller", layout.RenderedCacheRoot);
    }

    [Theory]
    [InlineData(null, @"C:\Users\Casey", "localApplicationDataPath")]
    [InlineData("", @"C:\Users\Casey", "localApplicationDataPath")]
    [InlineData("relative", @"C:\Users\Casey", "localApplicationDataPath")]
    [InlineData(@"C:\Local", null, "userProfilePath")]
    [InlineData(@"C:\Local", "", "userProfilePath")]
    [InlineData(@"C:\Local", "relative", "userProfilePath")]
    public void Create_RejectsMissingOrRelativeInputs(
        string? localApplicationDataPath,
        string? userProfilePath,
        string parameterName)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() =>
            LocalDataLayout.Create(localApplicationDataPath!, userProfilePath!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void Constructor_RejectsRelativeLayoutRoots()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new LocalDataLayout("relative", @"C:\Users\Casey\.waller"));

        Assert.Equal("appDataRoot", error.ParamName);
    }
}
