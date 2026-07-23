using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClipStash;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyManager _hotkeys = new();
    private AppConfig _config;
    private string? _lastSavedPath;
    private SettingsForm? _settingsForm;

    private readonly ToolStripMenuItem _saveItem;
    private readonly ToolStripMenuItem _openLastItem;
    private readonly ToolStripMenuItem _openFolderItem;
    private readonly ToolStripMenuItem _copyMarkdownItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _exitItem;

    public TrayApplicationContext()
    {
        _config = AppConfig.Load();
        L.Current = _config.Language;

        _saveItem = new ToolStripMenuItem("", null, (_, _) => SaveClipboard());
        _saveItem.Font = new Font(_saveItem.Font, FontStyle.Bold);

        _openLastItem = new ToolStripMenuItem("", null, (_, _) => OpenLastFile()) { Enabled = false };
        _openFolderItem = new ToolStripMenuItem("", null, (_, _) => OpenFolder());

        _copyMarkdownItem = new ToolStripMenuItem("") { CheckOnClick = true, Checked = _config.CopyMarkdownToClipboard };
        _copyMarkdownItem.CheckedChanged += (_, _) =>
        {
            _config.CopyMarkdownToClipboard = _copyMarkdownItem.Checked;
            _config.Save();
        };

        _startupItem = new ToolStripMenuItem("") { CheckOnClick = true, Checked = StartupManager.IsEnabled() };
        _startupItem.CheckedChanged += (_, _) => StartupManager.SetEnabled(_startupItem.Checked);

        _settingsItem = new ToolStripMenuItem("", null, (_, _) => OpenSettings());
        _exitItem = new ToolStripMenuItem("", null, (_, _) => ExitThread());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_saveItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_openLastItem);
        menu.Items.Add(_openFolderItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_copyMarkdownItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "ClipStash",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();

        _hotkeys.HotkeyPressed += SaveClipboard;
        ApplyConfig();
    }

    /// <summary>설정값을 UI(메뉴 라벨·언어)와 단축키 등록에 반영한다.</summary>
    private void ApplyConfig()
    {
        L.Current = _config.Language;
        ApplyStrings();
        _copyMarkdownItem.Checked = _config.CopyMarkdownToClipboard;

        if (!_hotkeys.TryRegister(_config.Hotkey, out string error))
            Notify(L.HotkeyErrorTitle, error, ToolTipIcon.Warning);
    }

    /// <summary>현재 언어로 모든 트레이 메뉴 라벨을 다시 설정한다.</summary>
    private void ApplyStrings()
    {
        _saveItem.Text = L.SaveClipboard(_config.Hotkey);
        _openLastItem.Text = L.OpenLastFile;
        _openFolderItem.Text = L.OpenFolder;
        _copyMarkdownItem.Text = L.CopyMarkdownAuto;
        _startupItem.Text = L.RunAtStartup;
        _settingsItem.Text = L.Settings;
        _exitItem.Text = L.Exit;
    }

    private void SaveClipboard()
    {
        try
        {
            SaveResult? result = ClipboardSaver.Save(_config);
            if (result is not { } saved)
            {
                Notify(L.SaveFailedTitle, L.NothingToSave, ToolTipIcon.Warning);
                return;
            }

            _lastSavedPath = saved.Path;
            _openLastItem.Enabled = true;

            string message = L.SavedTo(Path.GetDirectoryName(saved.Path) ?? "", Path.GetFileName(saved.Path));
            // 마크다운 태그는 이미지 저장 시에만 의미가 있다.
            if (saved.Kind == SavedKind.Image && _config.CopyMarkdownToClipboard)
            {
                Clipboard.SetText(_config.BuildMarkdown(saved.Path));
                message += "\n" + L.MarkdownCopied;
            }
            Notify("ClipStash", message, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            Notify(L.SaveFailedTitle, ex.Message, ToolTipIcon.Error);
        }
    }

    private void OpenLastFile()
    {
        if (_lastSavedPath is not null && File.Exists(_lastSavedPath))
            Process.Start(new ProcessStartInfo(_lastSavedPath) { UseShellExecute = true });
    }

    private void OpenFolder()
    {
        if (Directory.Exists(_config.SavePath))
            Process.Start(new ProcessStartInfo(_config.SavePath) { UseShellExecute = true });
        else
            Notify(L.FolderMissingTitle, L.FolderMissing(_config.SavePath), ToolTipIcon.Warning);
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        // 설정 창의 단축키 캡처 칸에서 현재 단축키 조합도 누를 수 있도록 잠시 해제한다.
        _hotkeys.Unregister();
        try
        {
            using var form = new SettingsForm(_config);
            _settingsForm = form;
            if (form.ShowDialog() == DialogResult.OK)
            {
                _config = form.Result;
                _config.Save();
            }
        }
        finally
        {
            _settingsForm = null;
            ApplyConfig(); // 저장이든 취소든 단축키를 복원/재등록한다.
        }
    }

    private void Notify(string title, string message, ToolTipIcon icon)
    {
        _trayIcon.ShowBalloonTip(3000, title, message, icon);
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _hotkeys.Dispose();
        base.ExitThreadCore();
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>단순한 카메라 모양 트레이 아이콘을 런타임에 그린다. (별도 리소스 파일 불필요)</summary>
    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var body = new SolidBrush(Color.FromArgb(0, 122, 204));
            g.FillRectangle(body, 11, 4, 10, 6);              // 상단 뷰파인더
            g.FillPath(body, RoundedRect(2, 8, 28, 20, 4));   // 본체
            g.FillEllipse(Brushes.White, 10, 12, 12, 12);     // 렌즈 테두리
            g.FillEllipse(body, 13, 15, 6, 6);                // 렌즈
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
