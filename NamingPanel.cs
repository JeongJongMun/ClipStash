namespace ClipStash;

/// <summary>
/// 이름 규칙 한 벌(저장 방식 + 옵션 + 접두/접미사)을 편집하는 재사용 패널.
/// 이미지 탭과 텍스트 탭이 각각 하나씩 가진다.
/// </summary>
public sealed class NamingPanel : FlowLayoutPanel
{
    private static readonly NamingMode[] Modes = { NamingMode.Number, NamingMode.DateTime, NamingMode.DateDaily };
    private static readonly PadWidth[] Pads = { PadWidth.None, PadWidth.Two, PadWidth.Three, PadWidth.Four };
    private static readonly DateStyle[] DateStyles = { DateStyle.Ymd8, DateStyle.Ymd6, DateStyle.YmdDash, DateStyle.YmdDash2, DateStyle.Md };
    private static readonly TimeStyle[] TimeStyles = { TimeStyle.None, TimeStyle.Hms, TimeStyle.HmsDash, TimeStyle.Hm };

    private readonly ComboBox _modeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly NumericUpDown _startNumber = new() { Width = 80, Minimum = 0, Maximum = 1_000_000 };
    private readonly ComboBox _numberPadding = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _dateStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _timeStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _dailyPadding = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _prefix = new() { Width = 200 };
    private readonly TextBox _suffix = new() { Width = 200 };

    private readonly Label _modeLabel = FieldLabel();
    private readonly Label _startNumberLabel = FieldLabel();
    private readonly Label _numberPaddingLabel = FieldLabel();
    private readonly Label _dateStyleLabel = FieldLabel();
    private readonly Label _timeStyleLabel = FieldLabel();
    private readonly Label _dailyPaddingLabel = FieldLabel();
    private readonly Label _prefixLabel = FieldLabel();
    private readonly Label _suffixLabel = FieldLabel();
    private readonly Label _previewLabel = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(6, 8, 3, 3), MaximumSize = new Size(500, 0) };

    private readonly FlowLayoutPanel _startNumberRow;
    private readonly FlowLayoutPanel _numberPaddingRow;
    private readonly FlowLayoutPanel _dateStyleRow;
    private readonly FlowLayoutPanel _timeStyleRow;
    private readonly FlowLayoutPanel _dailyPaddingRow;

    /// <summary>값이 바뀌어 미리보기를 다시 계산해야 할 때 발생.</summary>
    public event Action? Changed;

    public NamingPanel()
    {
        FlowDirection = FlowDirection.TopDown;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Padding = new Padding(6);

        _startNumberRow = Row(_startNumberLabel, _startNumber);
        _numberPaddingRow = Row(_numberPaddingLabel, _numberPadding);
        _dateStyleRow = Row(_dateStyleLabel, _dateStyle);
        _timeStyleRow = Row(_timeStyleLabel, _timeStyle);
        _dailyPaddingRow = Row(_dailyPaddingLabel, _dailyPadding);

        Controls.Add(Row(_modeLabel, _modeCombo));
        Controls.Add(_startNumberRow);
        Controls.Add(_numberPaddingRow);
        Controls.Add(_dateStyleRow);
        Controls.Add(_timeStyleRow);
        Controls.Add(_dailyPaddingRow);
        Controls.Add(Row(_prefixLabel, _prefix));
        Controls.Add(Row(_suffixLabel, _suffix));
        Controls.Add(_previewLabel);

        _modeCombo.SelectedIndexChanged += (_, _) => { UpdateModeVisibility(); Changed?.Invoke(); };
        foreach (var c in new[] { _numberPadding, _dateStyle, _timeStyle, _dailyPadding })
            c.SelectedIndexChanged += (_, _) => Changed?.Invoke();
        foreach (var c in new Control[] { _prefix, _suffix })
            c.TextChanged += (_, _) => Changed?.Invoke();
        _startNumber.ValueChanged += (_, _) => Changed?.Invoke();
    }

    /// <summary>규칙 값을 컨트롤에 채운다.</summary>
    public void LoadFrom(NamingConfig naming)
    {
        FillEnumCombo(_modeCombo, Modes, naming.NamingMode, m => L.NamingModeName(m));
        FillEnumCombo(_numberPadding, Pads, naming.NumberPadding, p => L.PadName(p));
        FillEnumCombo(_dateStyle, DateStyles, naming.DateStyle, s => L.DateStyleName(s));
        FillEnumCombo(_timeStyle, TimeStyles, naming.TimeStyle, t => L.TimeStyleName(t));
        FillEnumCombo(_dailyPadding, Pads, naming.DailyPadding, p => L.PadName(p));
        _startNumber.Value = Math.Clamp(naming.NumberStart, 0, 1_000_000);
        _prefix.Text = naming.NamePrefix;
        _suffix.Text = naming.NameSuffix;
        UpdateModeVisibility();
    }

    /// <summary>현재 컨트롤 상태를 규칙 객체로 만든다.</summary>
    public NamingConfig ToConfig() => new()
    {
        NamingMode = Modes[_modeCombo.SelectedIndex],
        NumberStart = (int)_startNumber.Value,
        NumberPadding = Pads[_numberPadding.SelectedIndex],
        DateStyle = DateStyles[_dateStyle.SelectedIndex],
        TimeStyle = TimeStyles[_timeStyle.SelectedIndex],
        DailyPadding = Pads[_dailyPadding.SelectedIndex],
        NamePrefix = _prefix.Text,
        NameSuffix = _suffix.Text,
    };

    public void SetPreview(string text) => _previewLabel.Text = text;

    /// <summary>현재 언어로 라벨과 콤보 항목을 다시 채운다.</summary>
    public void ApplyStrings()
    {
        _modeLabel.Text = L.NamingModeLabel;
        _startNumberLabel.Text = L.StartNumberLabel;
        _numberPaddingLabel.Text = L.PaddingLabel;
        _dateStyleLabel.Text = L.DateStyleLabel;
        _timeStyleLabel.Text = L.TimeStyleLabel;
        _dailyPaddingLabel.Text = L.PaddingLabel;
        _prefixLabel.Text = L.PrefixLabel;
        _suffixLabel.Text = L.SuffixLabel;

        FillEnumCombo(_modeCombo, Modes, Modes[_modeCombo.SelectedIndex], m => L.NamingModeName(m));
        FillEnumCombo(_numberPadding, Pads, Pads[_numberPadding.SelectedIndex], p => L.PadName(p));
        FillEnumCombo(_timeStyle, TimeStyles, TimeStyles[_timeStyle.SelectedIndex], t => L.TimeStyleName(t));
        FillEnumCombo(_dailyPadding, Pads, Pads[_dailyPadding.SelectedIndex], p => L.PadName(p));
    }

    private void UpdateModeVisibility()
    {
        if (_modeCombo.SelectedIndex < 0) return;
        var mode = Modes[_modeCombo.SelectedIndex];
        _startNumberRow.Visible = mode == NamingMode.Number;
        _numberPaddingRow.Visible = mode == NamingMode.Number;
        _dateStyleRow.Visible = mode is NamingMode.DateTime or NamingMode.DateDaily;
        _timeStyleRow.Visible = mode == NamingMode.DateTime;
        _dailyPaddingRow.Visible = mode == NamingMode.DateDaily;
    }

    private static Label FieldLabel()
        => new() { AutoSize = false, Width = 110, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 3, 6, 3), Height = 25 };

    private static FlowLayoutPanel Row(Control label, Control control)
    {
        var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0) };
        control.Margin = new Padding(0, 3, 3, 3);
        row.Controls.Add(label);
        row.Controls.Add(control);
        return row;
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
}
