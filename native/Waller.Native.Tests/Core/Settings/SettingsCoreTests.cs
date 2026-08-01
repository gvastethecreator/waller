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
    public void AppLanguages_NormalizesSupportedLanguageCodes()
    {
        Assert.Equal(AppLanguages.English, AppLanguages.NormalizeOrDefault("EN"));
        Assert.Equal(AppLanguages.Spanish, AppLanguages.NormalizeOrDefault("es"));
        Assert.Equal(AppLanguages.English, AppLanguages.NormalizeOrDefault("fr"));
        Assert.Contains(AppLanguages.English, AppLanguages.Supported);
        Assert.Contains(AppLanguages.Spanish, AppLanguages.Supported);
        Assert.Equal("en", AppLanguages.CultureFor("EN").Name);
        Assert.Equal("es", AppLanguages.CultureFor("ES").Name);
        Assert.Equal("en", AppLanguages.CultureFor("fr").Name);
    }

    [Fact]
    public async Task UserSettingsStore_RoundTripsSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            var settings = UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
                LastSelectedPresetId = Guid.NewGuid(),
            };

            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();

            Assert.Equal(AppThemePreference.Dark, loaded.Theme);
            Assert.Equal("es", loaded.Language);
            Assert.Equal(settings.LastSelectedPresetId, loaded.LastSelectedPresetId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_LoadMissingSettingsReturnsDefaultsThroughRecoverableRead()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default, loaded);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-root")]
    public void UserSettingsStore_RejectsInvalidRootDirectory(string? rootDirectory)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new UserSettingsStore(rootDirectory!));

        Assert.Equal("rootDirectory", error.ParamName);
    }

    [Fact]
    public async Task UserSettingsStore_SaveRejectsNullSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            UserSettings? settings = null;

            var error = await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(settings!));

            Assert.Equal("settings", error.ParamName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("load")]
    [InlineData("save")]
    public async Task UserSettingsStore_CancelledOperationsDoNotCreateLocalFiles(string action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => action switch
            {
                "load" => store.LoadAsync(cts.Token),
                "save" => store.SaveAsync(UserSettings.Default, cts.Token),
                _ => throw new InvalidOperationException(action),
            });

            Assert.False(File.Exists(Path.Combine(root, "settings.json")));
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_KeepsExistingJsonWhenAtomicSaveCannotReplaceFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
            });
            lockedStream = new FileStream(
                Path.Combine(root, "settings.json"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var error = await Record.ExceptionAsync(() => store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Light,
                Language = "en",
            }));

            Assert.True(error is IOException or UnauthorizedAccessException);
            lockedStream.Dispose();
            lockedStream = null;

            var loaded = await store.LoadAsync();
            var tempFiles = Directory.EnumerateFiles(root, "*.tmp").ToList();

            Assert.Equal(AppThemePreference.Dark, loaded.Theme);
            Assert.Equal("es", loaded.Language);
            Assert.Empty(tempFiles);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_FallsBackWhenLocalJsonIsCorrupt()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{ broken");
            var store = new UserSettingsStore(root);

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default, loaded);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_FallsBackWhenLocalJsonIsLocked()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
            });
            lockedStream = new FileStream(
                Path.Combine(root, "settings.json"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default, loaded);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_NormalizesUnsupportedLocalValues()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = (AppThemePreference)999,
                Language = "fr",
                WindowWidth = 10,
                WindowHeight = 20,
            });

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default.Theme, loaded.Theme);
            Assert.Equal(AppLanguages.English, loaded.Language);
            Assert.Equal(UserSettingsPolicy.MinWindowWidth, loaded.WindowWidth);
            Assert.Equal(UserSettingsPolicy.MinWindowHeight, loaded.WindowHeight);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_SavesCanonicalLanguageCode()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with { Language = "ES" });

            var loaded = await store.LoadAsync();

            Assert.Equal("es", loaded.Language);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_DropsEmptyLastSelectedPresetId()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with { LastSelectedPresetId = Guid.Empty });

            var loaded = await store.LoadAsync();

            Assert.Null(loaded.LastSelectedPresetId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_DropsIncompleteWindowPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                WindowX = 120,
                WindowY = null,
            });

            var loaded = await store.LoadAsync();

            Assert.Null(loaded.WindowX);
            Assert.Null(loaded.WindowY);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void UserSettingsPolicy_NormalizeRejectsNullSettings()
    {
        UserSettings? settings = null;

        var error = Assert.Throws<ArgumentNullException>(() => UserSettingsPolicy.Normalize(settings!));

        Assert.Equal("settings", error.ParamName);
    }

    [Fact]
    public void UserSettings_ConvertsNullLanguageToEmptyDraftValue()
    {
        var settings = new UserSettings(
            AppThemePreference.System,
            null,
            UserSettingsPolicy.DefaultWindowWidth,
            UserSettingsPolicy.DefaultWindowHeight,
            null,
            null,
            null);

        Assert.Equal(string.Empty, settings.Language);
    }

    [Fact]
    public void UserSettings_DefaultsToDarkWithoutAnExplicitThemeChoice()
    {
        Assert.Equal(AppThemePreference.Dark, UserSettings.Default.Theme);
        Assert.False(UserSettings.Default.ThemePreferenceWasSet);
    }

    [Fact]
    public void UserSettingsPolicy_MigratesLegacyThemeToDark()
    {
        var legacyLight = UserSettings.Default with
        {
            Theme = AppThemePreference.Light,
            ThemePreferenceWasSet = false,
        };

        var normalized = UserSettingsPolicy.Normalize(legacyLight);

        Assert.Equal(AppThemePreference.Dark, normalized.Theme);
        Assert.False(normalized.ThemePreferenceWasSet);
    }

    [Fact]
    public void UserSettings_WithExpressionConvertsNullLanguageToEmptyDraftValue()
    {
        var settings = UserSettings.Default with { Language = null! };

        Assert.Equal(string.Empty, settings.Language);
    }

    [Fact]
    public void UserSettingsPolicy_NormalizesNullLanguageToDefault()
    {
        var settings = UserSettings.Default with { Language = null! };

        var normalized = UserSettingsPolicy.Normalize(settings);

        Assert.Equal(AppLanguages.English, normalized.Language);
    }

    [Fact]
    public void UserSettings_ConvertsEmptyLastSelectedPresetIdToNull()
    {
        var settings = UserSettings.Default with { LastSelectedPresetId = Guid.Empty };

        Assert.Null(settings.LastSelectedPresetId);
    }

    [Fact]
    public void UserSettingsPolicy_NormalizesThemeLanguageAndWindowPlacement()
    {
        var normalized = UserSettingsPolicy.Normalize(UserSettings.Default with
        {
            Theme = (AppThemePreference)999,
            Language = "ES",
            WindowWidth = 1,
            WindowHeight = 2,
            WindowX = 100,
            WindowY = null,
        });

        Assert.Equal(UserSettings.Default.Theme, normalized.Theme);
        Assert.Equal(AppLanguages.Spanish, normalized.Language);
        Assert.Equal(UserSettingsPolicy.MinWindowWidth, normalized.WindowWidth);
        Assert.Equal(UserSettingsPolicy.MinWindowHeight, normalized.WindowHeight);
        Assert.Null(normalized.WindowX);
        Assert.Null(normalized.WindowY);
    }

    [Fact]
    public void UserSettings_WithWindowPlacementSetsCompletePlacement()
    {
        var updated = UserSettings.Default.WithWindowPlacement(1280, 720, -20, 40);

        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public void UserSettings_WithWindowPlacementClampsMinimumSize()
    {
        var updated = UserSettings.Default.WithWindowPlacement(1, 2, -20, 40);

        Assert.Equal(UserSettingsPolicy.MinWindowWidth, updated.WindowWidth);
        Assert.Equal(UserSettingsPolicy.MinWindowHeight, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public void WindowPlacementPolicy_CentersNewDefaultInWorkArea()
    {
        var placement = WindowPlacementPolicy.Resolve(
            UserSettings.Default,
            workAreaX: 0,
            workAreaY: 0,
            workAreaWidth: 1920,
            workAreaHeight: 1040);

        Assert.Equal(UserSettingsPolicy.DefaultWindowWidth, placement.Width);
        Assert.Equal(UserSettingsPolicy.DefaultWindowHeight, placement.Height);
        Assert.Equal(192, placement.X);
        Assert.Equal(8, placement.Y);
    }

    [Fact]
    public void WindowPlacementPolicy_MigratesLegacyDefaultAndIgnoresItsOffset()
    {
        var settings = UserSettings.Default with
        {
            WindowWidth = 1120,
            WindowHeight = 760,
            WindowX = 8,
            WindowY = 0,
        };

        var placement = WindowPlacementPolicy.Resolve(settings, 0, 0, 1920, 1040);

        Assert.Equal(UserSettingsPolicy.DefaultWindowWidth, placement.Width);
        Assert.Equal(UserSettingsPolicy.DefaultWindowHeight, placement.Height);
        Assert.Equal(192, placement.X);
        Assert.Equal(8, placement.Y);
    }

    [Fact]
    public void WindowPlacementPolicy_MigratesInterimDefaultAndRecentersIt()
    {
        var settings = UserSettings.Default with
        {
            WindowWidth = 1520,
            WindowHeight = 960,
            WindowX = 960,
            WindowY = 215,
        };

        var placement = WindowPlacementPolicy.Resolve(settings, 0, 0, 3440, 1400);

        Assert.Equal(1536, placement.Width);
        Assert.Equal(1024, placement.Height);
        Assert.Equal(952, placement.X);
        Assert.Equal(188, placement.Y);
    }

    [Fact]
    public void WindowPlacementPolicy_PreservesCustomPlacement()
    {
        var settings = UserSettings.Default.WithWindowPlacement(1360, 820, -80, 65);

        var placement = WindowPlacementPolicy.Resolve(settings, 0, 0, 1920, 1040);

        Assert.Equal(new WindowPlacement(1360, 820, -80, 65), placement);
    }

    [Fact]
    public void WindowPlacementPolicy_ClampsCenteredDefaultToSmallWorkArea()
    {
        var placement = WindowPlacementPolicy.Resolve(
            UserSettings.Default,
            workAreaX: 100,
            workAreaY: 50,
            workAreaWidth: 1280,
            workAreaHeight: 720);

        Assert.Equal(1280, placement.Width);
        Assert.Equal(720, placement.Height);
        Assert.Equal(100, placement.X);
        Assert.Equal(50, placement.Y);
    }

    [Fact]
    public void UserSettings_WithPreferencesPreservesWindowPlacement()
    {
        var presetId = Guid.NewGuid();
        var current = UserSettings.Default.WithWindowPlacement(1280, 720, -20, 40);

        var updated = current.WithPreferences(
            AppThemePreference.Dark,
            AppLanguages.Spanish,
            presetId);

        Assert.Equal(AppThemePreference.Dark, updated.Theme);
        Assert.Equal(AppLanguages.Spanish, updated.Language);
        Assert.Equal(presetId, updated.LastSelectedPresetId);
        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
        Assert.True(updated.ThemePreferenceWasSet);
    }

    [Fact]
    public void UserSettings_WithPreferencesPreservesExplicitLightTheme()
    {
        var updated = UserSettings.Default.WithPreferences(
            AppThemePreference.Light,
            AppLanguages.English,
            lastSelectedPresetId: null);

        var normalized = UserSettingsPolicy.Normalize(updated);

        Assert.Equal(AppThemePreference.Light, normalized.Theme);
        Assert.True(normalized.ThemePreferenceWasSet);
    }

    [Fact]
    public void UserSettings_WithPreferencesNormalizesLanguage()
    {
        var updated = UserSettings.Default.WithPreferences(
            AppThemePreference.Dark,
            "ES",
            lastSelectedPresetId: null);

        Assert.Equal(AppLanguages.Spanish, updated.Language);
    }

    [Fact]
    public void UserSettings_WithPreferencesConvertsEmptyLastSelectedPresetIdToNull()
    {
        var updated = UserSettings.Default.WithPreferences(
            AppThemePreference.Dark,
            AppLanguages.Spanish,
            Guid.Empty);

        Assert.Null(updated.LastSelectedPresetId);
    }

    [Fact]
    public void UserSettings_WithPreferencesRejectsInvalidTheme()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserSettings.Default.WithPreferences(
                (AppThemePreference)999,
                AppLanguages.Spanish,
                lastSelectedPresetId: null));

        Assert.Equal("theme", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    public void UserSettings_WithPreferencesRejectsUnsupportedLanguage(string? language)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            UserSettings.Default.WithPreferences(
                AppThemePreference.Dark,
                language!,
                lastSelectedPresetId: null));

        Assert.Equal("language", error.ParamName);
    }

    [Fact]
    public void UserSettings_WithLastSelectedPresetPreservesPreferencesAndWindowPlacement()
    {
        var presetId = Guid.NewGuid();
        var current = UserSettings.Default
            .WithWindowPlacement(1280, 720, -20, 40)
            .WithPreferences(AppThemePreference.Dark, AppLanguages.Spanish, null);

        var updated = current.WithLastSelectedPreset(presetId);

        Assert.Equal(AppThemePreference.Dark, updated.Theme);
        Assert.Equal(AppLanguages.Spanish, updated.Language);
        Assert.Equal(presetId, updated.LastSelectedPresetId);
        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public void UserSettings_WithLastSelectedPresetConvertsEmptyIdToNull()
    {
        var updated = UserSettings.Default.WithLastSelectedPreset(Guid.Empty);

        Assert.Null(updated.LastSelectedPresetId);
    }
}
