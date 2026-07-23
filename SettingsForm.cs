namespace ClipStash;

/// <summary>
/// config.json의 모든 항목을 편집하는 설정 창.
/// [기본] 언어·단축키 / [파일 이름] 이미지·텍스트 탭에서 각각 독립된 이름 규칙 / [이미지] 저장 위치·마크다운 / [텍스트] 저장 위치·확장자.
/// 각 이름 규칙 탭과 이미지·텍스트 카테고리에 저장 예시를 실시간으로 보여주고, '설정 초기화'로 기본값 복원이 가능하다.
/// 언어 드롭다운을 바꾸면 창의 모든 문구가 즉시 그 언어로 바뀐다.
/// </summary>
public sealed class SettingsForm : Form
{
    private static readonly TextExtension[] TextExts = { TextExtension.Txt, TextExtension.Md };
    private static readonly ImageFormatKind[] ImageFormats = { ImageFormatKind.Png, ImageFormatKind.Jpg };

    // 입력 컨트롤
    private readonly ComboBox _languageCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _savePathBox = new() { Width = 300 };
    private readonly ComboBox _imageFormatCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _hotkeyBox = new() { Width = 300, ReadOnly = true, BackColor = SystemColors.Window, Cursor = Cursors.Hand };
    private readonly TextBox _textPathBox = new() { Width = 300 };
    private readonly ComboBox _textExtCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly CheckBox _copyMarkdownCheck = new() { AutoSize = true, Margin = new Padding(6, 8, 3, 3) };
    private readonly TextBox _urlPrefixBox = new() { Width = 300 };
    private readonly TextBox _templateBox = new() { Width = 300 };

    // 이름 규칙 탭 (이미지/텍스트 각각 독립)
    private readonly TabControl _namingTabs = new() { Width = 530, Height = 250, Margin = new Padding(6) };
    private readonly TabPage _imageNamingTab = new();
    private readonly TabPage _textNamingTab = new();
    private readonly NamingPanel _imageNaming = new();
    private readonly NamingPanel _textNaming = new();

    // 라벨/버튼 (언어 전환 시 다시 채움)
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
    private readonly Button _browseImageButton = new() { AutoSize = true };
    private readonly Button _browseTextButton = new() { AutoSize = true };
    private readonly Button _resetButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
    private readonly Button _saveButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
    private readonly Button _cancelButton = new() { AutoSize = true, Padding = new Padding(10, 2, 10, 2), DialogResult = DialogResult.Cancel };

    private readonly GroupBox _generalGroup = MakeGroup();
    private readonly GroupBox _namingGroup = MakeGroup();
    private readonly GroupBox _imageGroup = MakeGroup();
    private readonly GroupBox _textGroup = MakeGroup();

    /// <summary>저장 버튼으로 닫혔을 때(DialogResult.OK) 편집 결과.</summary>
    public AppConfig Result { get; private set; }

    public SettingsForm(AppConfig current)
    {
        Result = current;

        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Icon = TrayApplicationContext.LoadAppIcon();

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

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        BuildLayout();
        ApplyStrings();
        UpdatePreview();
    }

    private void BuildLayout()
    {
        _generalGroup.Controls.Add(Stack(
            Row(_languageLabel, _languageCombo),
            Row(_hotkeyLabel, _hotkeyBox),
            _hotkeyHintLabel));

        _imageNamingTab.Controls.Add(_imageNaming);
        _textNamingTab.Controls.Add(_textNaming);
        _namingTabs.TabPages.Add(_imageNamingTab);
        _namingTabs.TabPages.Add(_textNamingTab);
        _namingGroup.Controls.Add(Stack(_namingTabs));

        _imageGroup.Controls.Add(Stack(
            Row(_savePathLabel, _savePathBox, _browseImageButton),
            Row(_imageFormatLabel, _imageFormatCombo),
            HorizontalRule(),
            _markdownSectionLabel,
            _copyMarkdownCheck,
            Row(_urlPrefixLabel, _urlPrefixBox),
            Row(_templateLabel, _templateBox)));

        _textGroup.Controls.Add(Stack(
            Row(_textFolderLabel, _textPathBox, _browseTextButton),
            _textFolderHintLabel,
            Row(_textExtLabel, _textExtCombo)));

        // 하단: 왼쪽에 초기화, 오른쪽에 저장/취소
        var bottom = new Panel { Height = 38, Margin = new Padding(0, 8, 0, 0) };
        _resetButton.Location = new Point(0, 6);
        var rightButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 3, 0, 0),
        };
        rightButtons.Controls.Add(_saveButton);
        rightButtons.Controls.Add(_cancelButton);
        bottom.Controls.Add(_resetButton);
        bottom.Controls.Add(rightButtons);

        var root = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(12) };
        foreach (var g in new Control[] { _generalGroup, _namingGroup, _imageGroup, _textGroup, bottom })
        {
            g.Width = 560;
            root.Controls.Add(g);
        }
        Controls.Add(root);
    }

    // ── 레이아웃 헬퍼 ──
    private static GroupBox MakeGroup() => new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
    private static Label FieldLabel() => new() { AutoSize = false, Width = 110, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 3, 6, 3), Height = 25 };
    private static Label HintLabel() => new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(6, 0, 3, 6) };

    /// <summary>그룹 안에서 하위 구획의 제목으로 쓰는 굵은 라벨.</summary>
    private static Label SectionLabel()
        => new() { AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold), Margin = new Padding(6, 0, 3, 4) };

    /// <summary>구획을 나누는 가로 실선.</summary>
    private static Panel HorizontalRule()
        => new() { Height = 1, Width = 520, BackColor = SystemColors.ControlDark, Margin = new Padding(6, 10, 3, 8) };

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
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Dock = DockStyle.Fill, Padding = new Padding(6) };
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
        _generalGroup.Text = L.GroupGeneral;
        _namingGroup.Text = L.GroupNaming;
        _imageGroup.Text = L.GroupImage;
        _textGroup.Text = L.GroupText;
        _imageNamingTab.Text = L.TabImage;
        _textNamingTab.Text = L.TabText;

        _languageLabel.Text = L.LanguageLabel;
        _savePathLabel.Text = L.SaveFolderLabel;
        _imageFormatLabel.Text = L.ImageFormatLabel;
        _hotkeyLabel.Text = L.HotkeyLabel;
        _hotkeyHintLabel.Text = L.HotkeyHint;
        _markdownSectionLabel.Text = L.MarkdownSection;
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
            Warn(L.EnterSaveFolder);
            return;
        }
        if (!HotkeyManager.TryParse(cfg.Hotkey, out _, out _, out string error))
        {
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

    private void Warn(string message)
        => MessageBox.Show(this, message, L.SettingsDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private bool ConfirmMissingFolder(string path)
        => MessageBox.Show(this, L.FolderNotExistConfirm(path), L.SettingsDialogTitle,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
