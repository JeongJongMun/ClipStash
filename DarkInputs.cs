using System.Runtime.InteropServices;

namespace ClipStash;

/// <summary>
/// 입력 컨트롤의 테두리를 베이지로 덧그리는 공통 로직.
/// TextBox·NumericUpDown의 테두리는 비클라이언트 영역이라 색을 직접 지정할 수 없어,
/// 기본 그리기가 끝난 뒤 창 DC에 직접 그린다.
/// </summary>
internal static class BorderPainter
{
    public const int WM_PAINT = 0x000F;
    public const int WM_NCPAINT = 0x0085;

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    public static void Draw(Control control)
    {
        if (!control.IsHandleCreated) return;

        IntPtr hdc = GetWindowDC(control.Handle);
        if (hdc == IntPtr.Zero) return;
        try
        {
            using var g = Graphics.FromHdc(hdc);
            using var pen = new Pen(Theme.Line);
            g.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
        }
        finally
        {
            ReleaseDC(control.Handle, hdc);
        }
    }
}

/// <summary>테두리가 베이지인 텍스트 입력.</summary>
public sealed class DarkTextBox : TextBox
{
    public DarkTextBox() => BorderStyle = BorderStyle.FixedSingle;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is BorderPainter.WM_PAINT or BorderPainter.WM_NCPAINT)
            BorderPainter.Draw(this);
    }
}

/// <summary>테두리가 베이지인 숫자 입력.</summary>
public sealed class DarkNumericUpDown : NumericUpDown
{
    public DarkNumericUpDown() => BorderStyle = BorderStyle.FixedSingle;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is BorderPainter.WM_PAINT or BorderPainter.WM_NCPAINT)
            BorderPainter.Draw(this);
    }
}
