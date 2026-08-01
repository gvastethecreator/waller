using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;
using Waller.Native.Core.Topology;
using Waller.Native.Core.Windows;

namespace Waller.Native.Tests;

public sealed partial class CoreArchitectureTests
{

    [Fact]
    public void DefinedEnumValue_ValidatesSupportedEnumValues()
    {
        Assert.True(DefinedEnumValue.IsDefined(WallpaperSourceKind.Empty));
        Assert.False(DefinedEnumValue.IsDefined((WallpaperSourceKind)999));
        Assert.Equal(
            WallpaperSourceKind.Image,
            DefinedEnumValue.Require(
                WallpaperSourceKind.Image,
                "sourceKind",
                "Source kind is invalid."));

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DefinedEnumValue.Require(
                (WallpaperSourceKind)999,
                "sourceKind",
                "Source kind is invalid."));
        Assert.Equal("sourceKind", error.ParamName);
        Assert.Contains("Source kind is invalid.", error.Message);
    }

    [Fact]
    public void ColorHexValue_NormalizesAndParsesRgbValues()
    {
        var color = ColorHexValue.Parse(" A1b2C3 ");

        Assert.Equal("#a1b2c3", color.ToHex());
        Assert.Equal(0xa1, color.Red);
        Assert.Equal(0xb2, color.Green);
        Assert.Equal(0xc3, color.Blue);
        Assert.Equal("#a1b2c3", ColorHexValue.Normalize("A1B2C3"));
    }

    [Fact]
    public void ColorHexValue_RejectsInvalidValuesWithoutThrowingInTryParse()
    {
        Assert.Throws<ArgumentException>(() => ColorHexValue.Parse("#12345"));

        Assert.False(ColorHexValue.TryParse("#12345", out _));
        Assert.False(ColorHexValue.TryParse("not-a-color", out _));
        Assert.False(ColorHexValue.TryParse(null, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ColorHexValue_RejectsMissingValues(string? colorHex)
    {
        var error = Assert.Throws<ArgumentException>(() => ColorHexValue.Normalize(colorHex!));

        Assert.Equal("colorHex", error.ParamName);
        Assert.Contains("Color must be #RRGGBB.", error.Message);
    }

    [Fact]
    public void WallpaperSourcePath_TryNormalizeImagePathReportsInvalidPaths()
    {
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath("   ", out _));
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath("relative\\wallpaper.png", out _));
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(@"C:\Wallpapers\notes.txt", out _));

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @" C:\Wallpapers\current.jpg ",
            out var normalized));
        Assert.Equal(@"C:\Wallpapers\current.jpg", normalized);

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\CURRENT.PNG",
            out var upperExtension));
        Assert.Equal(@"C:\Wallpapers\CURRENT.PNG", upperExtension);
    }

    [Fact]
    public void WallpaperSourcePath_TryNormalizeImagePathReportsErrorCodes()
    {
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            "   ",
            out _,
            out var blankError));
        Assert.Equal(WallpaperSourcePathException.Required, blankError?.ErrorCode);

        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            @"wallpapers\current.jpg",
            out _,
            out var relativeError));
        Assert.Equal(WallpaperSourcePathException.FullyQualifiedRequired, relativeError?.ErrorCode);

        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\current.txt",
            out _,
            out var unsupportedError));
        Assert.Equal(WallpaperSourcePathException.UnsupportedFileType, unsupportedError?.ErrorCode);

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\current.jpg",
            out var normalized,
            out var validError));
        Assert.Equal(@"C:\Wallpapers\current.jpg", normalized);
        Assert.Null(validError);
    }

    [Fact]
    public void WallpaperImageFileTypes_ExposeCommonPickerExtensions()
    {
        Assert.Contains(".jpg", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".jpeg", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".png", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".bmp", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".webp", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".gif", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".tif", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".tiff", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".heic", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".heif", WallpaperImageFileTypes.PickerExtensions);
        Assert.All(
            WallpaperImageFileTypes.PickerExtensions,
            extension => Assert.StartsWith(".", extension, StringComparison.Ordinal));
    }

    [Fact]
    public void MonitorIdentity_ReportsWhetherItCanBeUsedInPresetAssignments()
    {
        Assert.True(new MonitorIdentity("DISPLAY-1", null, 1, 1920, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity(" ", null, 1, 1920, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity("DISPLAY-1", null, 1, 0, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity("DISPLAY-1", null, 1, 1920, 0, 0, 0).IsValidForPresetAssignment);
    }

    [Fact]
    public void MonitorIdentity_NullKeyBecomesInvalidPresetAssignment()
    {
        var identity = new MonitorIdentity(null!, null, 1, 1920, 1080, 0, 0);

        Assert.Equal(string.Empty, identity.MonitorKey);
        Assert.False(identity.IsValidForPresetAssignment);
    }

    [Fact]
    public void WallpaperPlacement_RejectsInvalidFitMode()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperPlacement((WallpaperFitMode)999, WallpaperAnchor.Center));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_RejectsInvalidAnchor()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperPlacement(WallpaperFitMode.Cover, (WallpaperAnchor)999));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_WithExpressionRejectsInvalidFitMode()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperPlacement.Default with { FitMode = (WallpaperFitMode)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_WithExpressionRejectsInvalidAnchor()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperPlacement.Default with { Anchor = (WallpaperAnchor)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsInvalidKind()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperSource((WallpaperSourceKind)999));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_WithExpressionRejectsInvalidKind()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperSource.Empty with { Kind = (WallpaperSourceKind)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_TryNormalizeValidatesPayload()
    {
        Assert.Equal(
            WallpaperSource.Empty,
            WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.Empty, "relative\\ignored.png")));
        Assert.Equal(
            "#aabbcc",
            WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: "AABBCC"))?.ColorHex);
        Assert.True(WallpaperSource.TryNormalize(
            new WallpaperSource(WallpaperSourceKind.Image, @"C:\Wallpapers\legacy.png")) is { Kind: WallpaperSourceKind.Image });
        Assert.Null(WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.Image, "relative\\wallpaper.png")));
        Assert.Null(WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: "bad")));
    }

    [Fact]
    public void WallpaperSource_NormalizesFullImagePath()
    {
        var source = WallpaperSource.FromImage(@"  C:\Wallpapers\current.jpg  ");

        Assert.Equal(@"C:\Wallpapers\current.jpg", source.ImagePath);
    }

    [Fact]
    public void WallpaperSource_RejectsRelativeImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage(@"wallpapers\current.jpg"));

        Assert.Equal(WallpaperSourcePathException.FullyQualifiedRequired, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsUnsupportedImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage(@"C:\Wallpapers\current.txt"));

        Assert.Equal(WallpaperSourcePathException.UnsupportedFileType, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsBlankImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage("   "));

        Assert.Equal(WallpaperSourcePathException.Required, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public async Task WallpaperSourceFiles_DetectsExistingAndMissingImageFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-source-file-{Guid.NewGuid():N}.png");
        try
        {
            var source = WallpaperSource.FromImage(path);
            Assert.True(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));

            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.True(WallpaperSourceFiles.HasExistingImageFile(source));
            Assert.Equal(Path.GetFileName(path), WallpaperSourceFiles.ImageFileName(source));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task WallpaperSourceFiles_NormalizesImagePathsBeforeFileChecks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-source-file-{Guid.NewGuid():N}.png");
        try
        {
            var source = new WallpaperSource(WallpaperSourceKind.Image, $" {path} ");
            Assert.True(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
            Assert.Equal(Path.GetFileName(path), WallpaperSourceFiles.ImageFileName(source));

            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.True(WallpaperSourceFiles.HasExistingImageFile(source));
            Assert.Equal(Path.GetFileName(path), WallpaperSourceFiles.ImageFileName(source));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void WallpaperSourceFiles_IgnoresInvalidImagePaths()
    {
        var source = new WallpaperSource(WallpaperSourceKind.Image, "relative\\wallpaper.png");

        Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
        Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
        Assert.Null(WallpaperSourceFiles.ImageFileName(source));
    }

    [Fact]
    public void WallpaperSourceFiles_IgnoresNonImageSources()
    {
        var source = WallpaperSource.FromSolidColor("#112233");

        Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
        Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
        Assert.Null(WallpaperSourceFiles.ImageFileName(source));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("existing")]
    [InlineData("file-name")]
    public void WallpaperSourceFiles_RejectsNullSource(string operation)
    {
        WallpaperSource? source = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = operation switch
            {
                "missing" => WallpaperSourceFiles.IsMissingImageFile(source!),
                "existing" => WallpaperSourceFiles.HasExistingImageFile(source!),
                "file-name" => WallpaperSourceFiles.ImageFileName(source!) is not null,
                _ => throw new InvalidOperationException($"Unknown operation: {operation}"),
            };
        });

        Assert.Equal("source", error.ParamName);
    }
}
