using System.Text.Json.Serialization;
using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(Preset))]
[JsonSerializable(typeof(UserSettings))]
internal sealed partial class WallerJsonContext : JsonSerializerContext;
