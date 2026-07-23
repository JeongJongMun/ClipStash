using System.Runtime.InteropServices;

namespace ClipStash;

/// <summary>
/// 앱 색상과 다크 테마 적용을 한 곳에서 관리한다.
/// 기본 두 색(다크 그레이 배경 + 베이지 글자)에서 표면·경계·흐린 글자색을 파생시킨다.
/// 컨트롤에 <see cref="Control.Tag"/>로 역할("muted", "rule", "nav", "primary")을 지정하면 그에 맞게 칠한다.
/// </summary>
public static class Theme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#1A1A1A");
    public static readonly Color Accent = ColorTranslator.FromHtml("#F5EFE0");

    /// <summary>사이드바처럼 배경과 살짝 구분해야 하는 면.</summary>
    public static readonly Color Surface = ColorTranslator.FromHtml("#141414");

    /// <summary>입력 칸 배경. 배경보다 밝게 해서 입력 영역임을 드러낸다.</summary>
    public static readonly Color Field = ColorTranslator.FromHtml("#262626");

    /// <summary>보조 설명용 흐린 글자 (베이지를 배경 쪽으로 섞음).</summary>
    public static readonly Color Muted = Blend(Accent, Background, 0.60);

    /// <summary>구분선·테두리.</summary>
    public static readonly Color Border = Blend(Accent, Background, 0.22);

    /// <summary>役할 태그. Tag에 넣으면 <see cref="Apply"/>가 알아본다.</summary>
    public const string Muted_ = "muted";
    public const string Rule = "rule";
    public const string Nav = "nav";
    public const string Primary = "primary";

    private static Color Blend(Color a, Color b, double ratio) => Color.FromArgb(
        (int)(a.R * ratio + b.R * (1 - ratio)),
        (int)(a.G * ratio + b.G * (1 - ratio)),
        (int)(a.B * ratio + b.B * (1 - ratio)));

    /// <summary>컨트롤 트리 전체에 테마를 적용한다.</summary>
    public static void Apply(Control control)
    {
        switch (control)
        {
            case TextBox textBox:
                textBox.BackColor = Field;
                textBox.ForeColor = Accent;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                return;

            case ComboBox combo:
                // DropDownList는 비주얼 스타일이 직접 그려서 BackColor를 무시한다.
                // 컨트롤에서 테마를 벗겨내야(SetWindowTheme) 지정한 색이 실제로 칠해진다.
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = Field;
                combo.ForeColor = Accent;
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem -= DrawComboItem;
                combo.DrawItem += DrawComboItem;
                Unthemed(combo);
                return;

            case NumericUpDown numeric:
                numeric.BackColor = Field;
                numeric.ForeColor = Accent;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                // 내부 UpDownButtons에서 테마를 벗기면 렌더링이 깨지므로 색만 바꾼다.
                foreach (Control child in numeric.Controls)
                    child.BackColor = Field;
                return;                        // 내부 컨트롤은 더 건드리지 않는다

            case CheckBox check:
                check.FlatStyle = FlatStyle.Flat;
                check.BackColor = Background;
                check.ForeColor = Accent;
                check.FlatAppearance.BorderColor = Border;
                check.FlatAppearance.CheckedBackColor = Accent;
                break;

            case Button button:
                if ((string?)button.Tag != Nav)   // 네비 버튼은 선택 상태에 따라 따로 칠한다
                    StyleButton(button, (string?)button.Tag == Primary);
                break;

            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = (string?)label.Tag == Muted_ ? Muted : Accent;
                break;

            case Panel panel:
                panel.BackColor = (string?)panel.Tag switch
                {
                    Rule => Border,
                    _ => panel.BackColor == SystemColors.ControlLight ? Surface : Background,
                };
                break;

            default:
                control.BackColor = Background;
                control.ForeColor = Accent;
                break;
        }

        foreach (Control child in control.Controls)
            Apply(child);
    }

    /// <summary>콤보박스 항목을 직접 그린다. 선택된 항목은 베이지 반전.</summary>
    private static void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo) return;

        // 닫힌 상태의 표시 영역(ComboBoxEdit)에도 DrawItem이 호출된다.
        // 이때는 '선택됨' 상태로 오므로 강조색을 쓰면 안 된다. 강조는 펼친 목록에서만.
        bool inEditArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
        bool highlighted = !inEditArea && (e.State & DrawItemState.Selected) != 0;

        using (var background = new SolidBrush(highlighted ? Accent : Field))
            e.Graphics.FillRectangle(background, e.Bounds);

        if (e.Index >= 0)
        {
            using var text = new SolidBrush(highlighted ? Background : Accent);
            e.Graphics.DrawString(combo.Items[e.Index]?.ToString(), e.Font ?? combo.Font, text,
                e.Bounds.Left + 2, e.Bounds.Top + 1);
        }
    }

    /// <summary>일반 버튼: 어두운 바탕 + 베이지 테두리. primary는 베이지 채움.</summary>
    public static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Accent : Background;
        button.ForeColor = primary ? Background : Accent;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary ? Blend(Accent, Background, 0.85) : Field;
    }

    /// <summary>선택/비선택 상태의 사이드바 버튼 색을 칠한다.</summary>
    public static void StyleNavButton(Button button, bool selected)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = selected ? Accent : Surface;
        button.ForeColor = selected ? Background : Accent;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = selected ? Accent : Field;
        button.Font = new Font(button.Font, selected ? FontStyle.Bold : FontStyle.Regular);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

    /// <summary>
    /// 컨트롤에서 비주얼 스타일을 제거해 지정한 BackColor/ForeColor가 실제로 칠해지게 한다.
    /// 핸들이 아직 없으면 생성 시점에 적용한다.
    /// </summary>
    private static void Unthemed(Control control)
    {
        if (control.IsHandleCreated)
            SetWindowTheme(control.Handle, "", "");
        else
            control.HandleCreated += (s, _) => SetWindowTheme(((Control)s!).Handle, "", "");
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Windows 11 제목 표시줄을 어둡게. (지원하지 않는 버전에서는 조용히 무시된다)</summary>
    public static void UseDarkTitleBar(Form form)
    {
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        int enabled = 1;
        try { DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)); }
        catch (DllNotFoundException) { /* 구버전 Windows */ }
    }
}
