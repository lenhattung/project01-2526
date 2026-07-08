using System.Globalization;
using System.Runtime.InteropServices;

namespace StudentForm;

internal static class RemoteInputService
{
    private const int MouseEventLeftDown = 0x0002;
    private const int MouseEventLeftUp = 0x0004;
    private const int MouseEventRightDown = 0x0008;
    private const int MouseEventRightUp = 0x0010;

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

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int flags, int dx, int dy, int data, UIntPtr extraInfo);
}
