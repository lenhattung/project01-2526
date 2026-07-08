using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StudentForm;

internal sealed class ClipboardShortcutBlocker : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkC = 0x43;
    private const int VkV = 0x56;
    private const int VkInsert = 0x2D;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;

    private readonly LowLevelKeyboardProc _callback;
    private readonly Action<string> _blocked;
    private IntPtr _hookId = IntPtr.Zero;

    public ClipboardShortcutBlocker(Action<string> blocked)
    {
        _blocked = blocked;
        _callback = HookCallback;
    }

    public bool IsRunning => _hookId != IntPtr.Zero;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? currentModule = currentProcess.MainModule;
        _hookId = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(currentModule?.ModuleName), 0);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WmKeyDown || wParam == WmSysKeyDown))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            bool control = (GetAsyncKeyState(VkControl) & 0x8000) != 0;
            bool shift = (GetAsyncKeyState(VkShift) & 0x8000) != 0;
            bool blocked = control && (vkCode == VkC || vkCode == VkV || vkCode == VkInsert)
                || shift && vkCode == VkInsert;

            if (blocked)
            {
                _blocked(vkCode == VkC ? "copy" : "paste");
                return 1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
