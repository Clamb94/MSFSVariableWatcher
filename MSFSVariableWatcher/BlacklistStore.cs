using System.Text.Json;

namespace MSFSVariableWatcher
{
    /// <summary>
    /// Persists the LVAR blacklist to disk so it survives between launches.
    /// Stored at %LOCALAPPDATA%\MSFSVariableWatcher\blacklist.json. All IO is best-effort:
    /// failures are logged and swallowed so they never break the UI.
    ///
    /// Every page circuit (i.e. every open browser tab) holds its own in-memory copy, so all
    /// mutations go through <see cref="Add"/> / <see cref="Clear"/>, which read-modify-write the
    /// file under a process-wide lock and hand back the merged set. A blind Save of a stale
    /// in-memory copy would drop entries another tab had added in the meantime.
    /// </summary>
    public static class BlacklistStore
    {
        private static readonly object Gate = new();

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFSVariableWatcher",
            "blacklist.json");

        public static HashSet<string> Load()
        {
            lock (Gate)
            {
                return LoadUnlocked();
            }
        }

        /// <summary>Merges <paramref name="names"/> into the persisted set and returns the result.</summary>
        public static HashSet<string> Add(IEnumerable<string> names)
        {
            lock (Gate)
            {
                var merged = LoadUnlocked();
                merged.UnionWith(names);
                SaveUnlocked(merged);
                return merged;
            }
        }

        /// <summary>Empties the persisted set and returns the (empty) result.</summary>
        public static HashSet<string> Clear()
        {
            lock (Gate)
            {
                var empty = new HashSet<string>();
                SaveUnlocked(empty);
                return empty;
            }
        }

        private static HashSet<string> LoadUnlocked()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new HashSet<string>();
                }

                var json = File.ReadAllText(FilePath);
                var names = JsonSerializer.Deserialize<List<string>>(json);
                return names is null ? new HashSet<string>() : new HashSet<string>(names);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load blacklist: {ex.Message}");
                return new HashSet<string>();
            }
        }

        private static void SaveUnlocked(IEnumerable<string> names)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json = JsonSerializer.Serialize(names.ToList(),
                    new JsonSerializerOptions { WriteIndented = true });

                // Write-then-rename: a crash mid-write would otherwise leave a truncated file,
                // which Load() cannot parse and silently reports as an empty blacklist.
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, FilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save blacklist: {ex.Message}");
            }
        }
    }
}
