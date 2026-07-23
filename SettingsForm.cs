namespace EasyClipStash;

/// <summary>
/// config.json의 모든 항목을 편집하는 설정 창.
/// 왼쪽 카테고리 버튼(기본 / 파일 이름 / 이미지 / 텍스트)으로 오른쪽 페이지를 전환한다.
/// 각 페이지는 AutoScroll이라 옵션이 늘어나도 잘리지 않는다.
/// 언어 드롭다운을 바꾸면 창의 모든 문구가 즉시 그 언어로 바뀐다.
/// </summary>
public sealed class SettingsForm : Form
{
    private static readonly TextExtension[] TextExts = { TextExtension.Txt, TextExtension.Md };
    private static readonly ImageFormatKind[] ImageFormats = { ImageFormatKind.Png, ImageFormatKind.Jpg };

    private const int SidebarWidth = 160;

    // 입력 컨트롤
    private readonly DarkComboBox _languageCombo = new() { Width = 200 };
    private readonly DarkTextBox _savePathBox = new() { Width = 300 };
    private readonly DarkComboBox _imageFormatCombo = new() { Width = 200 };
    private readonly DarkTextBox _hotkeyBox = new() { Width = 300, ReadOnly = true, BackColor = SystemColors.Window, Cursor = Cursors.Hand };
    private readonly DarkTextBox _textPathBox = new() { Width = 300 };
    private readonly DarkComboBox _textExtCombo = new() { Width = 200 };
    private readonly CheckBox _copyMarkdownCheck = new() { AutoSize = true, Margin = new Padding(6, 8, 3, 3) };
    private readonly DarkTextBox _urlPrefixBox = new() { Width = 300 };
    private readonly DarkTextBox _templateBox = new() { Width = 300 };

    // 이름 규칙 (이미지/텍스트 각각 독립) — OS가 그리는 TabControl은 다크로 칠할 수 없어
    // 사이드바와 같은 토글 버튼 방식으로 전환한다.
    private readonly Button[] _namingTabButtons = new Button[2];
    private readonly NamingPanel _imageNaming = new();
    private readonly NamingPanel _textNaming = new();
    private int _currentNamingTab = -1;

    // 라벨/버튼
    private readonly Label _languageLabel = FieldLabel();
    private readonly Label _savePathLabel = FieldLabel();
    private readonly Label _imageFormatLabel = FieldLabel();
    private readonly Label _hotkeyLabel = FieldLabel();
    private readonly Label _hotkeyHintLabel = HintLabel();
    private readonly Label _markdownSectionLabel = SectionLabel();
    private readonly Label _textFolderLabel = FieldLabel();
    private readonly Label _textFolderHintLabel = HintLabel();
    private readonly Label _textExtLabel = FieldLabel();
    private readonly Label _urlPrefixLabel = FieldLabel();
    private readonly Label _templateLabel = FieldLabel();
    // 업데이트
    private readonly Label _updateSectionLabel = SectionLabel();
    private readonly Label _currentVersionLabel = FieldLabel();
    private readonly Label _currentVersionValue = new() { AutoSize = true, Margin = new Padding(0, 6, 3, 3) };
    private readonly CheckBox _autoCheckUpdateCheck = new() { AutoSize = true, Margin = new Padding(6, 4, 3, 6) };
    private readonly Button _checkUpdateButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2), Margin = new Padding(6, 3, 8, 3) };
    private readonly Button _updateNowButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2), Margin = new Padding(0, 3, 8, 3), Visible = false, Tag = Theme.Primary };
    private readonly Label _updateStatusLabel = HintLabel();
    private UpdateInfo? _pendingUpdate;

    private readonly Button _browseImageButton = new() { AutoSize = true };
    private readonly Button _browseTextButton = new() { AutoSize = true };
    private readonly Button _resetButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
    private readonly Button _saveButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2), Tag = Theme.Primary };
    private readonly Button _cancelButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2), DialogResult = DialogResult.Cancel };

    // 좌측 카테고리 네비게이션과 대응하는 페이지
    private readonly Button[] _navButtons = new Button[4];
    private readonly Panel[] _pages = new Panel[4];
    private readonly Label[] _pageTitles = new Label[4];
    private int _currentPage = -1;

    /// <summary>저장 버튼으로 닫혔을 때(DialogResult.OK) 편집 결과.</summary>
    public AppConfig Result { get; private set; }

    public SettingsForm(AppConfig current)
    {
        Result = current;

        Text = L.SettingsWindowTitle;
        Icon = TrayApplicationContext.LoadAppIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(740, 460);   // 페이지 전환 시 창 크기가 흔들리지 않도록 고정

        _languageCombo.Items.AddRange(new object[] { L.DisplayName(Lang.Korean), L.DisplayName(Lang.English) });

        LoadFromConfig(current);

        // ── 이벤트 ──
        _hotkeyBox.KeyDown += OnHotkeyBoxKeyDown;
        _languageCombo.SelectedIndexChanged += (_, _) => { L.Current = SelectedLanguage; ApplyStrings(); UpdatePreview(); };
        foreach (var c in new Control[] { _savePathBox, _textPathBox, _urlPrefixBox, _templateBox })
            c.TextChanged += (_, _) => UpdatePreview();
        _textExtCombo.SelectedIndexChanged += (_, _) => UpdatePreview();
        _imageFormatCombo.SelectedIndexChanged += (_, _) => UpdatePreview();
        _copyMarkdownCheck.CheckedChanged += (_, _) => UpdateMarkdownEnabled();
        _imageNaming.Changed += UpdatePreview;
        _textNaming.Changed += UpdatePreview;
        _browseImageButton.Click += (_, _) => Browse(_savePathBox, L.FolderPickerDescription);
        _browseTextButton.Click += (_, _) => Browse(_textPathBox, L.TextFolderPickerDescription);
        _resetButton.Click += (_, _) => ResetToDefaults();
        _saveButton.Click += (_, _) => TrySaveAndClose();
        _checkUpdateButton.Click += async (_, _) => await CheckForUpdateAsync();
        _updateNowButton.Click += async (_, _) => await ApplyUpdateAsync();

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        BuildLayout();
        Theme.Apply(this);
        ApplyStrings();
        SelectPage(0);        // 테마 적용 후에 호출해야 선택 상태 색이 유지된다
        SelectNamingTab(0);
        UpdatePreview();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.UseDarkTitleBar(this);
    }

    private void BuildLayout()
    {
        // 페이지 0: 기본 (+ 업데이트 섹션)
        var updateButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        updateButtons.Controls.Add(_checkUpdateButton);
        updateButtons.Controls.Add(_updateNowButton);
        _updateStatusLabel.Margin = new Padding(6, 10, 3, 6);
        updateButtons.Controls.Add(_updateStatusLabel);

        _pages[0] = Page(0, Stack(
            Row(_languageLabel, _languageCombo),
            Row(_hotkeyLabel, _hotkeyBox),
            _hotkeyHintLabel,
            HorizontalRule(),
            _updateSectionLabel,
            Row(_currentVersionLabel, _currentVersionValue),
            _autoCheckUpdateCheck,
            updateButtons));

        // 페이지 1: 파일 이름 (이미지/텍스트 전환)
        var namingTabBar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 10) };
        for (int i = 0; i < _namingTabButtons.Length; i++)
        {
            int index = i;
            var b = new Button
            {
                Width = 96,
                Height = 30,
                Tag = Theme.Nav,
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand,
            };
            b.Click += (_, _) => SelectNamingTab(index);
            _namingTabButtons[i] = b;
            namingTabBar.Controls.Add(b);
        }
        var namingBody = new Panel { Width = 520, Height = 250, Margin = new Padding(0) };
        _imageNaming.Dock = DockStyle.Fill;
        _textNaming.Dock = DockStyle.Fill;
        namingBody.Controls.Add(_imageNaming);
        namingBody.Controls.Add(_textNaming);
        _pages[1] = Page(1, Stack(namingTabBar, namingBody));

        // 페이지 2: 이미지 설정
        _pages[2] = Page(2, Stack(
            Row(_savePathLabel, _savePathBox, _browseImageButton),
            Row(_imageFormatLabel, _imageFormatCombo),
            HorizontalRule(),
            _markdownSectionLabel,
            _copyMarkdownCheck,
            Row(_urlPrefixLabel, _urlPrefixBox),
            Row(_templateLabel, _templateBox)));

        // 페이지 3: 텍스트 설정
        _pages[3] = Page(3, Stack(
            Row(_textFolderLabel, _textPathBox, _browseTextButton),
            _textFolderHintLabel,
            Row(_textExtLabel, _textExtCombo)));

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 12, 12) };
        foreach (var p in _pages) content.Controls.Add(p);

        // 좌측 카테고리 버튼
        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 12, 8, 8),
        };
        for (int i = 0; i < _navButtons.Length; i++)
        {
            int index = i;
            var b = new Button
            {
                Width = SidebarWidth - 24,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 4),
                Tag = Theme.Nav,
                Cursor = Cursors.Hand,
            };
            b.Click += (_, _) => SelectPage(index);
            _navButtons[i] = b;
            sidebar.Controls.Add(b);
        }

        // 하단: 왼쪽 초기화, 오른쪽 저장/취소
        var bottom = new Panel { Dock = DockStyle.Fill };
        _resetButton.Location = new Point(12, 8);
        var rightButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 5, 12, 0),
        };
        rightButtons.Controls.Add(_saveButton);
        rightButtons.Controls.Add(_cancelButton);
        bottom.Controls.Add(_resetButton);
        bottom.Controls.Add(rightButtons);
        bottom.Controls.Add(Theme.Divider(DockStyle.Top));    // 본문과 버튼 영역 구분

        // 사이드바는 배경색이 아니라 세로 베이지 선으로 본문과 구분한다
        var sidebarArea = new Panel { Dock = DockStyle.Fill };
        sidebarArea.Controls.Add(sidebar);
        sidebarArea.Controls.Add(Theme.Divider(DockStyle.Right));

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.Controls.Add(sidebarArea, 0, 0);
        root.Controls.Add(content, 1, 0);
        root.Controls.Add(bottom, 0, 1);
        root.SetColumnSpan(bottom, 2);

        Controls.Add(root);
    }

    /// <summary>제목 + 내용으로 구성된 카테고리 페이지 하나를 만든다.</summary>
    private Panel Page(int index, Control body)
    {
        var title = new Label
        {
            AutoSize = true,
            Font = new Font(DefaultFont.FontFamily, DefaultFont.Size + 2, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 10),
        };
        _pageTitles[index] = title;

        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };
        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        stack.Controls.Add(title);
        stack.Controls.Add(body);
        panel.Controls.Add(stack);
        return panel;
    }

    /// <summary>카테고리를 전환한다. 선택된 버튼을 강조하고 해당 페이지만 보인다.</summary>
    private void SelectPage(int index)
    {
        if (_currentPage == index) return;
        _currentPage = index;

        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == index;
            Theme.StyleNavButton(_navButtons[i], i == index);
        }
    }

    /// <summary>파일 이름 페이지 안에서 이미지/텍스트 규칙을 전환한다.</summary>
    private void SelectNamingTab(int index)
    {
        if (_currentNamingTab == index) return;
        _currentNamingTab = index;

        _imageNaming.Visible = index == 0;
        _textNaming.Visible = index == 1;
        for (int i = 0; i < _namingTabButtons.Length; i++)
            Theme.StyleNavButton(_namingTabButtons[i], i == index);
    }

    // ── 레이아웃 헬퍼 ──
    private static Label FieldLabel() => new() { AutoSize = false, Width = 110, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 3, 6, 3), Height = 25 };
    private static Label HintLabel() => new() { AutoSize = true, Tag = Theme.Muted_, Margin = new Padding(6, 0, 3, 6) };

    /// <summary>페이지 안에서 하위 구획의 제목으로 쓰는 굵은 라벨.</summary>
    private static Label SectionLabel()
        => new() { AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold), Margin = new Padding(6, 0, 3, 4) };

    /// <summary>구획을 나누는 가로 실선.</summary>
    private static Panel HorizontalRule()
        => new() { Height = 1, Width = 500, Tag = Theme.Rule, Margin = new Padding(6, 10, 3, 8) };

    private static FlowLayoutPanel Row(Control label, Control control, Control? extra = null)
    {
        var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0) };
        control.Margin = new Padding(0, 3, 3, 3);
        row.Controls.Add(label);
        row.Controls.Add(control);
        if (extra is not null) row.Controls.Add(extra);
        return row;
    }

    private static FlowLayoutPanel Stack(params Control[] children)
    {
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0) };
        stack.Controls.AddRange(children);
        return stack;
    }

    private static void FillEnumCombo<T>(ComboBox combo, T[] values, T selected, Func<T, string> name)
    {
        int index = Math.Max(0, Array.IndexOf(values, selected));
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (var v in values) combo.Items.Add(name(v));
        combo.SelectedIndex = index;
        combo.EndUpdate();
    }

    private Lang SelectedLanguage => _languageCombo.SelectedIndex == 0 ? Lang.Korean : Lang.English;

    /// <summary>설정값을 모든 입력 컨트롤에 채운다. (생성자와 '설정 초기화'가 공용)</summary>
    private void LoadFromConfig(AppConfig cfg)
    {
        _languageCombo.SelectedIndex = cfg.Language == Lang.Korean ? 0 : 1;
        _savePathBox.Text = cfg.SavePath;
        _hotkeyBox.Text = cfg.Hotkey;
        _textPathBox.Text = cfg.TextSavePath;
        _copyMarkdownCheck.Checked = cfg.CopyMarkdownToClipboard;
        _urlPrefixBox.Text = cfg.MarkdownUrlPrefix;
        _templateBox.Text = cfg.MarkdownTemplate;
        _autoCheckUpdateCheck.Checked = cfg.CheckUpdateOnStartup;
        FillEnumCombo(_textExtCombo, TextExts, cfg.TextExtension, e => L.TextExtName(e));
        FillEnumCombo(_imageFormatCombo, ImageFormats, cfg.ImageFormat, k => L.ImageFormatName(k));

        _imageNaming.LoadFrom(cfg.ImageNaming);
        _textNaming.LoadFrom(cfg.TextNaming);
        UpdateMarkdownEnabled();
    }

    /// <summary>마크다운 복사가 꺼져 있으면 관련 옵션(URL 경로·템플릿)을 비활성화한다.</summary>
    private void UpdateMarkdownEnabled()
    {
        bool on = _copyMarkdownCheck.Checked;
        _urlPrefixLabel.Enabled = on;
        _urlPrefixBox.Enabled = on;
        _templateLabel.Enabled = on;
        _templateBox.Enabled = on;
    }

    /// <summary>확인 후 모든 컨트롤을 기본값으로 되돌린다. '저장'을 눌러야 실제 반영된다.</summary>
    private void ResetToDefaults()
    {
        if (MessageBox.Show(this, L.ResetConfirm, L.SettingsDialogTitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var defaults = new AppConfig();
        L.Current = defaults.Language;
        LoadFromConfig(defaults);
        ApplyStrings();
        UpdatePreview();
    }

    /// <summary>현재 언어로 창의 모든 고정 문구를 다시 채운다. (콤보 항목은 선택 유지하며 재생성)</summary>
    private void ApplyStrings()
    {
        Text = L.SettingsWindowTitle;

        string[] categories = { L.GroupGeneral, L.GroupNaming, L.GroupImage, L.GroupText };
        for (int i = 0; i < categories.Length; i++)
        {
            _navButtons[i].Text = categories[i];
            _pageTitles[i].Text = categories[i];
        }

        _namingTabButtons[0].Text = L.TabImage;
        _namingTabButtons[1].Text = L.TabText;

        _languageLabel.Text = L.LanguageLabel;
        _savePathLabel.Text = L.SaveFolderLabel;
        _imageFormatLabel.Text = L.ImageFormatLabel;
        _hotkeyLabel.Text = L.HotkeyLabel;
        _hotkeyHintLabel.Text = L.HotkeyHint;
        _markdownSectionLabel.Text = L.MarkdownSection;
        _updateSectionLabel.Text = L.UpdateSection;
        _currentVersionLabel.Text = L.CurrentVersionLabel;
        _currentVersionValue.Text = Updater.CurrentVersion.ToString();
        _autoCheckUpdateCheck.Text = L.AutoCheckUpdate;
        _checkUpdateButton.Text = L.CheckUpdate;
        _updateNowButton.Text = L.UpdateNow;
        _textFolderLabel.Text = L.TextFolderLabel;
        _textFolderHintLabel.Text = L.TextFolderHint;
        _textExtLabel.Text = L.TextExtLabel;
        _urlPrefixLabel.Text = L.UrlPrefixLabel;
        _templateLabel.Text = L.TemplateLabel;
        _copyMarkdownCheck.Text = L.CopyMarkdownCheck;
        _browseImageButton.Text = L.Browse;
        _browseTextButton.Text = L.Browse;
        _resetButton.Text = L.ResetButton;
        _saveButton.Text = L.Save;
        _cancelButton.Text = L.Cancel;

        _imageNaming.ApplyStrings();
        _textNaming.ApplyStrings();
    }

    private void OnHotkeyBoxKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.None)
            return;

        var parts = new List<string>();
        if (e.Control) parts.Add("Ctrl");
        if (e.Alt) parts.Add("Alt");
        if (e.Shift) parts.Add("Shift");
        parts.Add(e.KeyCode.ToString());
        _hotkeyBox.Text = string.Join("+", parts);
    }

    private void Browse(TextBox target, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(target.Text) ? target.Text : "",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            target.Text = dialog.SelectedPath;
    }

    /// <summary>현재 컨트롤 상태를 그대로 담은 설정(미리보기·저장 공용). 검증은 하지 않는다.</summary>
    private AppConfig BuildConfigFromUi() => new()
    {
        SavePath = _savePathBox.Text.Trim(),
        ImageFormat = ImageFormats[_imageFormatCombo.SelectedIndex],
        Hotkey = _hotkeyBox.Text,
        Language = SelectedLanguage,
        CheckUpdateOnStartup = _autoCheckUpdateCheck.Checked,
        CopyMarkdownToClipboard = _copyMarkdownCheck.Checked,
        MarkdownUrlPrefix = _urlPrefixBox.Text.Trim(),
        MarkdownTemplate = _templateBox.Text,
        ImageNaming = _imageNaming.ToConfig(),
        TextNaming = _textNaming.ToConfig(),
        TextSavePath = _textPathBox.Text.Trim(),
        TextExtension = TextExts[_textExtCombo.SelectedIndex],
    };

    /// <summary>이미지·텍스트의 다음 파일명을 각 이름 규칙 탭에 표시한다.</summary>
    private void UpdatePreview()
    {
        try
        {
            var cfg = BuildConfigFromUi();
            var now = DateTime.Now;
            _imageNaming.SetPreview(L.ImagePreview(
                FileNamer.PreviewName(cfg.ImageNaming, cfg.SavePath, FileNamer.Extension(cfg.ImageFormat), now)));
            _textNaming.SetPreview(L.TextPreview(
                FileNamer.PreviewName(cfg.TextNaming, cfg.EffectiveTextFolder, FileNamer.Extension(cfg.TextExtension), now)));
        }
        catch (Exception ex)
        {
            _imageNaming.SetPreview(ex.Message);
            _textNaming.SetPreview(ex.Message);
        }
    }

    private void TrySaveAndClose()
    {
        var cfg = BuildConfigFromUi();

        if (cfg.SavePath.Length == 0)
        {
            SelectPage(2);
            Warn(L.EnterSaveFolder);
            return;
        }
        if (!HotkeyManager.TryParse(cfg.Hotkey, out _, out _, out string error))
        {
            SelectPage(0);
            Warn(error);
            return;
        }
        if (!Directory.Exists(cfg.SavePath) && !ConfirmMissingFolder(cfg.SavePath))
            return;
        if (cfg.TextSavePath.Length > 0 && !Directory.Exists(cfg.TextSavePath) && !ConfirmMissingFolder(cfg.TextSavePath))
            return;

        Result = cfg;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>최신 릴리스를 조회해 상태를 표시한다. 새 버전이 있으면 [지금 업데이트]를 노출한다.</summary>
    private async Task CheckForUpdateAsync()
    {
        _checkUpdateButton.Enabled = false;
        _updateNowButton.Visible = false;
        _pendingUpdate = null;
        _updateStatusLabel.Text = L.UpdateChecking;
        try
        {
            _pendingUpdate = await Updater.CheckAsync();
            if (_pendingUpdate is null)
            {
                _updateStatusLabel.Text = L.UpToDate;
            }
            else
            {
                _updateStatusLabel.Text = L.UpdateAvailable(_pendingUpdate.Version);
                _updateNowButton.Visible = Updater.IsSupported;
                if (!Updater.IsSupported)
                    _updateStatusLabel.Text += "  " + L.UpdateNotSupported;
            }
        }
        catch (Exception ex)
        {
            _updateStatusLabel.Text = L.UpdateFailed(ex.Message);
        }
        finally
        {
            _checkUpdateButton.Enabled = true;
        }
    }

    /// <summary>내려받아 검증한 뒤 교체하고 재시작한다. 성공하면 앱이 종료된다.</summary>
    private async Task ApplyUpdateAsync()
    {
        if (_pendingUpdate is not { } update) return;

        _updateNowButton.Enabled = false;
        _checkUpdateButton.Enabled = false;
        try
        {
            var progress = new Progress<int>(p => _updateStatusLabel.Text = L.UpdateDownloading(p));
            await Updater.DownloadAndApplyAsync(update, progress);
            _updateStatusLabel.Text = L.UpdateRestarting;
        }
        catch (Exception ex)
        {
            _updateStatusLabel.Text = L.UpdateFailed(ex.Message);
            _updateNowButton.Enabled = true;
            _checkUpdateButton.Enabled = true;
        }
    }

    private void Warn(string message)
        => MessageBox.Show(this, message, L.SettingsDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private bool ConfirmMissingFolder(string path)
        => MessageBox.Show(this, L.FolderNotExistConfirm(path), L.SettingsDialogTitle,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
