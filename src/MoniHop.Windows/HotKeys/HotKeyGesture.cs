using System.Text.Json.Serialization;

namespace MoniHop.Windows.HotKeys;

[Flags]
public enum HotKeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
}

public readonly record struct HotKeyGesture
{
    [JsonConstructor]
    public HotKeyGesture(HotKeyModifiers modifiers, uint virtualKey)
    {
        if (modifiers == HotKeyModifiers.None)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiers), "At least one modifier is required.");
        }

        if (virtualKey == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualKey), "A virtual key is required.");
        }

        Modifiers = modifiers;
        VirtualKey = virtualKey;
    }

    public HotKeyModifiers Modifiers { get; }

    public uint VirtualKey { get; }
}
