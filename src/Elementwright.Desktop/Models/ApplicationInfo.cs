namespace Elementwright.Desktop.Models;

public sealed record ApplicationInfo(
    int ProcessId,
    string ProcessName,
    string WindowTitle,
    string ExecutablePath,
    nint MainWindowHandle);
