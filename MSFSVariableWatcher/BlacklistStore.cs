using System.Text.Json;

namespace MSFSVariableWatcher
{
    /// <summary>
    /// Persists the LVAR blacklist to disk so it survives between launches.
    /// Stored at %LOCALAPPDATA%\MSFSVariableWatcher\blacklist.json. All IO is best-effort:
    /// failures are logged and swallowed so they never break the UI.
    /// </summary>
    public static class BlacklistStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSFSVariableWatcher",
            "blacklist.json");

        public static HashSet<string> Load()
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

        public static void Save(IEnumerable<string> names)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json = JsonSerializer.Serialize(names.ToList(),
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save blacklist: {ex.Message}");
            }
        }
    }
}
