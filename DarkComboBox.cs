using System.Drawing.Drawing2D;

namespace EasyClipStash;

/// <summary>
/// 다크 테마용 콤보박스.
/// 본체 배경·항목 글자는 소유자 그리기로 칠할 수 있지만 테두리와 드롭다운 버튼은
/// 네이티브가 밝게 그려버리므로, 기본 그리기가 끝난 뒤 그 부분만 덧그린다.
/// </summary>
public sealed class DarkComboBox : ComboBox
{
    private const int WM_PAINT = 0x000F;
    private const int ButtonWidth = 18;

    public DarkComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        FlatStyle = FlatStyle.Flat;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_PAINT && IsHandleCreated)
            PaintChrome();
    }

    private void PaintChrome()
    {
        using var g = Graphics.FromHwnd(Handle);

        // 드롭다운 버튼 영역을 배경색으로 덮고 화살표를 다시 그린다
        var button = new Rectangle(Width - ButtonWidth - 1, 1, ButtonWidth, Height - 2);
        using (var fill = new SolidBrush(Theme.Background))
            g.FillRectangle(fill, button);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        int cx = button.Left + button.Width / 2;
        int cy = button.Top + button.Height / 2;
        using (var arrow = new SolidBrush(Theme.Accent))
            g.FillPolygon(arrow, new[]
            {
                new Point(cx - 4, cy - 2),
                new Point(cx + 4, cy - 2),
                new Point(cx, cy + 3),
            });

        // 네이티브가 그린 밝은 테두리를 덮는다
        g.SmoothingMode = SmoothingMode.None;
        using (var pen = new Pen(Theme.Line))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
