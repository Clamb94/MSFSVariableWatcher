using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MSFSVariableWatcher
{
    /// <summary>
    /// A single MSFS key event ("K:" event) as documented in the SDK.
    /// </summary>
    public sealed class KEvent
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("group")] public string Group { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "";
        [JsonPropertyName("parameters")] public string Parameters { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("sims")] public List<string> Sims { get; set; } = new();

        /// <summary>
        /// Number of documented parameters, derived from the "[0]: ... [1]: ..." markers.
        /// </summary>
        [JsonIgnore]
        public int ParameterCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < 5; i++)
                {
                    if (Parameters.Contains($"[{i}]", StringComparison.Ordinal))
                    {
                        count = i + 1;
                    }
                }
                return count;
            }
        }
    }

    /// <summary>
    /// The static catalogue of K: events.
    ///
    /// K: events cannot be enumerated from a running sim - SimConnect only enumerates
    /// aircraft-specific Input Events (B: type), not key events. So the list is generated
    /// from the MSFS 2020 + 2024 SDK docs by tools/gen-kevents and embedded in the exe.
    /// </summary>
    public static class KEventCatalog
    {
        private const string ResourceName = "MSFSVariableWatcher.Data.KEvents.json";

        private static readonly Lazy<IReadOnlyList<KEvent>> _events = new(Load);

        public static IReadOnlyList<KEvent> Events => _events.Value;

        private static readonly Lazy<IReadOnlyList<string>> _groups =
            new(() => Events.Select(e => e.Group).Distinct().OrderBy(g => g).ToList());

        public static IReadOnlyList<string> Groups => _groups.Value;

        private static IReadOnlyList<KEvent> Load()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
                if (stream is null)
                {
                    Console.WriteLine($"K: event catalogue resource '{ResourceName}' not found.");
                    return Array.Empty<KEvent>();
                }

                return JsonSerializer.Deserialize<List<KEvent>>(stream) ?? new List<KEvent>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load the K: event catalogue: {ex.Message}");
                return Array.Empty<KEvent>();
            }
        }
    }
}
