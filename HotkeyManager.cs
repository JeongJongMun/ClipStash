using System.Runtime.InteropServices;

namespace ClipStash;

/// <summary>
/// RegisterHotKey 기반 전역 단축키. 메시지 수신용 숨김 창(NativeWindow)을 하나 만들어 사용한다.
/// </summary>
public sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 1;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private bool _registered;

    public event Action? HotkeyPressed;

    public HotkeyManager()
    {
        CreateHandle(new CreateParams());
    }

    /// <summary>"Ctrl+Alt+V" 형식의 문자열을 RegisterHotKey용 modifier 비트와 가상 키로 파싱한다.</summary>
    public static bool TryParse(string hotkeyText, out uint modifiers, out Keys key, out string error)
    {
        error = string.Empty;
        modifiers = MOD_NOREPEAT;
        key = Keys.None;

        foreach (var token in hotkeyText.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= MOD_CONTROL; break;
                case "alt": modifiers |= MOD_ALT; break;
                case "shift": modifiers |= MOD_SHIFT; break;
                case "win" or "windows": modifiers |= MOD_WIN; break;
                default:
                    if (key != Keys.None || !Enum.TryParse(token, ignoreCase: true, out key) || !Enum.IsDefined(key))
                    {
                        error = L.CannotParseHotkey(token);
                        return false;
                    }
                    break;
            }
        }

        if (key == Keys.None)
        {
            error = L.NoMainKey(hotkeyText);
            return false;
        }

        return true;
    }

    /// <summary>"Ctrl+Alt+V" 형식의 문자열을 파싱해 전역 단축키로 등록한다.</summary>
    public bool TryRegister(string hotkeyText, out string error)
    {
        Unregister();

        if (!TryParse(hotkeyText, out uint modifiers, out Keys key, out error))
            return false;

        if (!RegisterHotKey(Handle, HotkeyId, modifiers, (uint)key))
        {
            error = L.HotkeyRegisterFailed(hotkeyText);
            return false;
        }

        _registered = true;
        return true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HotkeyId)
            HotkeyPressed?.Invoke();
        base.WndProc(ref m);
    }

    /// <summary>설정 창에서 단축키를 다시 캡처할 수 있도록 일시적으로 등록을 해제할 때도 쓴다.</summary>
    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
