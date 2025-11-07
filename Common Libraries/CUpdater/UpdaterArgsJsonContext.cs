using System.Text.Json.Serialization;

namespace Cybertron.CUpdater;

[JsonSourceGenerationOptions(WriteIndented = false, IncludeFields = false, UseStringEnumConverter = true)]
[JsonSerializable(typeof(UpdaterArgs))]
public partial class UpdaterArgsJsonContext : JsonSerializerContext;
