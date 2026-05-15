namespace WMover;

internal sealed class HotkeyBinding
{
    public WindowAction Action  { get; set; }
    public string       Label   { get; set; } = "";
    public bool         Ctrl    { get; set; }
    public bool         Shift   { get; set; }
    public bool         Alt     { get; set; }
    public bool         Win     { get; set; }
    public uint         VkCode  { get; set; }

    public string ShortcutText
    {
        get
        {
            var parts = new List<string>();
            if (Ctrl)  parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt)   parts.Add("Alt");
            if (Win)   parts.Add("Win");
            parts.Add(VkName(VkCode));
            return string.Join(" + ", parts);
        }
    }

    public static string VkName(uint vk) => vk switch
    {
        0x25 => "←",
        0x27 => "→",
        0x26 => "↑",
        0x28 => "↓",
        _    => ((Keys)vk).ToString(),
    };

    public static List<HotkeyBinding> Defaults() =>
    [
        new() { Action = WindowAction.HalfLeft,        Label = "1/2 width — snap left",  Win = true, Ctrl = false, VkCode = 0x25 },
        new() { Action = WindowAction.HalfRight,       Label = "1/2 width — snap right", Win = true, Ctrl = false, VkCode = 0x27 },
        new() { Action = WindowAction.TwoThirdsLeft,   Label = "2/3 width — snap left",  Win = true, Ctrl = true,  VkCode = 0x45 },
        new() { Action = WindowAction.TwoThirdsRight,  Label = "2/3 width — snap right", Win = true, Ctrl = true,  VkCode = 0x54 },
        new() { Action = WindowAction.TwoThirdsCenter, Label = "2/3 width — center",     Win = true, Ctrl = true,  VkCode = 0x52 },
        new() { Action = WindowAction.OneThirdLeft,    Label = "1/3 width — snap left",  Win = true, Ctrl = true,  VkCode = 0x44 },
        new() { Action = WindowAction.OneThirdRight,   Label = "1/3 width — snap right", Win = true, Ctrl = true,  VkCode = 0x47 },
        new() { Action = WindowAction.OneThirdCenter,  Label = "1/3 width — center",     Win = true, Ctrl = true,  VkCode = 0x46 },
        new() { Action = WindowAction.CenterOnly,      Label = "Center (keep size)",      Win = true, Ctrl = true,  VkCode = 0x43 },
    ];
}
