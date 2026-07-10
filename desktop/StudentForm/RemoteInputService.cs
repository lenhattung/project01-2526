using System.Globalization;
using System.Runtime.InteropServices;

namespace StudentForm;

internal static class RemoteInputService
{
    private const int MouseEventLeftDown = 0x0002;
    private const int MouseEventLeftUp = 0x0004;
    private const int MouseEventRightDown = 0x0008;
    private const int MouseEventRightUp = 0x0010;
    private const int MouseEventWheel = 0x0800;
    private const int KeyEventKeyUp = 0x0002;

    public static void Click(string relativeXValue, string relativeYValue, string button)
    {
        double relativeX = Parse(relativeXValue);
        double relativeY = Parse(relativeYValue);
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        int x = bounds.Left + (int)Math.Round(bounds.Width * Math.Clamp(relativeX, 0, 1));
        int y = bounds.Top + (int)Math.Round(bounds.Height * Math.Clamp(relativeY, 0, 1));

        SetCursorPos(x, y);
        if (button.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            mouse_event(MouseEventRightDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);
        }
        else
        {
            mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    public static void Pointer(string action, string relativeXValue, string relativeYValue, string button, string wheelDeltaValue)
    {
        (int x, int y) = ResolvePoint(relativeXValue, relativeYValue);
        SetCursorPos(x, y);

        int wheelDelta = int.TryParse(wheelDeltaValue, out int parsedWheel) ? parsedWheel : 0;
        bool right = button.Equals("right", StringComparison.OrdinalIgnoreCase);
        switch (action.ToLowerInvariant())
        {
            case "down":
                mouse_event(right ? MouseEventRightDown : MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                break;
            case "up":
                mouse_event(right ? MouseEventRightUp : MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                break;
            case "click":
                mouse_event(right ? MouseEventRightDown : MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(right ? MouseEventRightUp : MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
                break;
            case "wheel":
                mouse_event(MouseEventWheel, 0, 0, wheelDelta, UIntPtr.Zero);
                break;
            default:
                break;
        }
    }

    public static void Key(string action, string key, string text, string modifiers)
    {
        if (action.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            TypeText(text);
            return;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!TryMapVirtualKey(key, out byte virtualKey))
        {
            string sendKey = key.Length == 1 ? key : $"{{{key.ToUpperInvariant()}}}";
            SendKeys.SendWait(sendKey);
            return;
        }

        byte[] modifierKeys = ParseModifierKeys(modifiers);
        if (action.Equals("up", StringComparison.OrdinalIgnoreCase))
        {
            keybd_event(virtualKey, 0, KeyEventKeyUp, UIntPtr.Zero);
            foreach (byte modifierKey in modifierKeys.Reverse())
            {
                keybd_event(modifierKey, 0, KeyEventKeyUp, UIntPtr.Zero);
            }

            return;
        }

        foreach (byte modifierKey in modifierKeys)
        {
            keybd_event(modifierKey, 0, 0, UIntPtr.Zero);
        }

        keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
    }

    public static void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SendKeys.SendWait(text.Replace("+", "{+}", StringComparison.Ordinal));
    }

    private static double Parse(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : 0.5;
    }

    private static (int x, int y) ResolvePoint(string relativeXValue, string relativeYValue)
    {
        double relativeX = Parse(relativeXValue);
        double relativeY = Parse(relativeYValue);
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        int x = bounds.Left + (int)Math.Round(bounds.Width * Math.Clamp(relativeX, 0, 1));
        int y = bounds.Top + (int)Math.Round(bounds.Height * Math.Clamp(relativeY, 0, 1));
        return (x, y);
    }

    private static byte[] ParseModifierKeys(string modifiers)
    {
        List<byte> keys = [];
        foreach (string modifier in modifiers.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (modifier.ToUpperInvariant())
            {
                case "CTRL":
                    keys.Add(0x11);
                    break;
                case "SHIFT":
                    keys.Add(0x10);
                    break;
                case "ALT":
                    keys.Add(0x12);
                    break;
            }
        }

        return [.. keys];
    }

    private static bool TryMapVirtualKey(string key, out byte virtualKey)
    {
        if (key.Length == 1)
        {
            char value = char.ToUpperInvariant(key[0]);
            if ((value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9'))
            {
                virtualKey = (byte)value;
                return true;
            }
        }

        virtualKey = key.ToUpperInvariant() switch
        {
            "ENTER" => 0x0D,
            "ESC" => 0x1B,
            "TAB" => 0x09,
            "BACKSPACE" => 0x08,
            "DELETE" => 0x2E,
            "INSERT" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "SPACE" => 0x20,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            _ => (byte)0
        };
        return virtualKey != 0;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int flags, int dx, int dy, int data, UIntPtr extraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, int flags, UIntPtr extraInfo);
}
