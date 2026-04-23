using System.Text.Json;

namespace WMover;

internal static class AppSettings
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WMover", "settings.json");

    public static List<HotkeyBinding> Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var saved = JsonSerializer.Deserialize<List<SavedBinding>>(File.ReadAllText(_path));
                var defaults = HotkeyBinding.Defaults();
                if (saved != null && saved.Count == defaults.Count)
                    return saved.Select((s, i) => new HotkeyBinding
                    {
                        Action = s.Action,
                        Label  = defaults.First(d => d.Action == s.Action).Label,
                        Ctrl   = s.Ctrl,
                        Alt    = s.Alt,
                        Win    = s.Win,
                        VkCode = s.VkCode,
                    }).ToList();
            }
        }
        catch { }
        return HotkeyBinding.Defaults();
    }

    public static void Save(IReadOnlyList<HotkeyBinding> bindings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var saved = bindings.Select(b => new SavedBinding
            {
                Action = b.Action, Ctrl = b.Ctrl, Alt = b.Alt, Win = b.Win, VkCode = b.VkCode,
            }).ToList();
            File.WriteAllText(_path, JsonSerializer.Serialize(saved,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private sealed class SavedBinding
    {
        public WindowAction Action { get; set; }
        public bool         Ctrl   { get; set; }
        public bool         Alt    { get; set; }
        public bool         Win    { get; set; }
        public uint         VkCode { get; set; }
    }
}
