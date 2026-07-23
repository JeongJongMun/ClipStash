using Microsoft.Win32;

namespace ClipStash;

/// <summary>HKCU Run 키를 통한 로그인 시 자동 실행 등록/해제.</summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipStash";

    private static string ExePath => Application.ExecutablePath;

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string path
            && string.Equals(path.Trim('"'), ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
