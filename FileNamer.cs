namespace ClipStash;

/// <summary>파일 이름을 만드는 방식.</summary>
public enum NamingMode { Number, DateTime, DateDaily }

/// <summary>번호 앞에 0을 채우는 자릿수. 값이 곧 자릿수(None=채우지 않음).</summary>
public enum PadWidth { None = 0, Two = 2, Three = 3, Four = 4 }

/// <summary>날짜 표기 형식.</summary>
public enum DateStyle { Ymd8, Ymd6, YmdDash, YmdDash2, Md }

/// <summary>시간 표기 형식.</summary>
public enum TimeStyle { None, Hms, HmsDash, Hm }

/// <summary>텍스트 저장 확장자.</summary>
public enum TextExtension { Txt, Md }

/// <summary>이미지 저장 형식. (System.Drawing.Imaging.ImageFormat과 이름이 겹치지 않게 Kind를 붙임)</summary>
public enum ImageFormatKind { Png, Jpg }

/// <summary>파일 이름 규칙 한 벌. 이미지와 텍스트가 각각 하나씩 가진다.</summary>
public class NamingConfig
{
    public NamingMode NamingMode { get; set; } = NamingMode.Number;
    public int NumberStart { get; set; } = 0;
    public PadWidth NumberPadding { get; set; } = PadWidth.None;
    public DateStyle DateStyle { get; set; } = DateStyle.Ymd8;
    public TimeStyle TimeStyle { get; set; } = TimeStyle.Hms;
    public PadWidth DailyPadding { get; set; } = PadWidth.None;
    public string NamePrefix { get; set; } = "";
    public string NameSuffix { get; set; } = "";
}

/// <summary>
/// 이름 규칙에 따라 폴더 안에서 "다음에 쓸 파일 경로"를 만든다.
/// 규칙: (접두사)(핵심 이름)(접미사).확장자 — 핵심 이름은 저장 방식에 따라 번호/날짜_시간/날짜_순번.
/// </summary>
public static class FileNamer
{
    /// <summary>드롭다운 예시 표시에 쓰는 샘플 시각 (2000-10-17 21:00:59).</summary>
    public static readonly DateTime Sample = new(2000, 10, 17, 21, 0, 59);

    public static string DateFormat(DateStyle s) => s switch
    {
        DateStyle.Ymd8 => "yyyyMMdd",
        DateStyle.Ymd6 => "yyMMdd",
        DateStyle.YmdDash => "yyyy-MM-dd",
        DateStyle.YmdDash2 => "yy-MM-dd",
        DateStyle.Md => "MM-dd",
        _ => "yyyyMMdd",
    };

    public static string TimeFormat(TimeStyle t) => t switch
    {
        TimeStyle.Hms => "HHmmss",
        TimeStyle.HmsDash => "HH-mm-ss",
        TimeStyle.Hm => "HHmm",
        _ => "",
    };

    public static string Extension(TextExtension ext) => ext == TextExtension.Md ? ".md" : ".txt";

    public static string Extension(ImageFormatKind kind) => kind == ImageFormatKind.Jpg ? ".jpg" : ".png";

    /// <summary>folder 안에서 naming 규칙에 맞는, 아직 존재하지 않는 파일 경로를 만든다.</summary>
    public static string BuildNext(NamingConfig naming, string folder, string extension, DateTime now)
    {
        string core = naming.NamingMode switch
        {
            NamingMode.DateTime => DateTimeCore(naming, now),
            NamingMode.DateDaily => DateDailyCore(naming, folder, now),
            _ => NumberCore(naming, folder),
        };

        string prefix = Sanitize(naming.NamePrefix);
        string suffix = Sanitize(naming.NameSuffix);
        string baseName = prefix + core + suffix;

        string path = Path.Combine(folder, baseName + extension);
        // 같은 초에 두 번 저장하는 등 충돌 시 _2, _3… 을 붙여 덮어쓰기를 막는다.
        for (int dup = 2; File.Exists(path); dup++)
            path = Path.Combine(folder, $"{baseName}_{dup}{extension}");
        return path;
    }

    /// <summary>미리보기/저장에서 쓰는 "다음 파일명"(경로 제외).</summary>
    public static string PreviewName(NamingConfig naming, string folder, string extension, DateTime now)
        => Path.GetFileName(BuildNext(naming, folder, extension, now));

    private static string NumberCore(NamingConfig naming, string folder)
    {
        int start = naming.NumberStart;
        string prefix = Sanitize(naming.NamePrefix);
        string suffix = Sanitize(naming.NameSuffix);

        int max = int.MinValue;
        bool found = false;
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                if (TryExtractInt(Path.GetFileNameWithoutExtension(file), prefix, suffix, out int n))
                {
                    found = true;
                    if (n > max) max = n;
                }
            }
        }

        int next = found ? Math.Max(max + 1, start) : start;
        return Pad(next, naming.NumberPadding);
    }

    private static string DateTimeCore(NamingConfig naming, DateTime now)
    {
        string date = now.ToString(DateFormat(naming.DateStyle));
        string timeFmt = TimeFormat(naming.TimeStyle);
        return timeFmt.Length == 0 ? date : $"{date}_{now.ToString(timeFmt)}";
    }

    private static string DateDailyCore(NamingConfig naming, string folder, DateTime now)
    {
        string date = now.ToString(DateFormat(naming.DateStyle));
        string prefix = Sanitize(naming.NamePrefix);
        string suffix = Sanitize(naming.NameSuffix);
        string head = date + "_";

        int max = 0;
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!TryStripAffixes(name, prefix, suffix, out string core)) continue;
                if (core.StartsWith(head, StringComparison.Ordinal)
                    && int.TryParse(core.AsSpan(head.Length), out int k) && k > max)
                    max = k;
            }
        }

        return head + Pad(max + 1, naming.DailyPadding);
    }

    /// <summary>이름에서 접두사/접미사를 떼어낸 뒤 정수로 파싱한다.</summary>
    private static bool TryExtractInt(string name, string prefix, string suffix, out int value)
    {
        value = 0;
        return TryStripAffixes(name, prefix, suffix, out string core) && int.TryParse(core, out value);
    }

    private static bool TryStripAffixes(string name, string prefix, string suffix, out string core)
    {
        core = name;
        if (prefix.Length > 0)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            core = core[prefix.Length..];
        }
        if (suffix.Length > 0)
        {
            if (!core.EndsWith(suffix, StringComparison.Ordinal)) return false;
            core = core[..^suffix.Length];
        }
        return true;
    }

    private static string Pad(int number, PadWidth width)
        => width == PadWidth.None ? number.ToString() : number.ToString().PadLeft((int)width, '0');

    /// <summary>파일명에 쓸 수 없는 문자를 조용히 제거한다. (Easy 컨셉: 사용자가 신경 쓸 필요 없게)</summary>
    private static string Sanitize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        Span<char> buffer = stackalloc char[s.Length];
        int len = 0;
        var invalid = Path.GetInvalidFileNameChars();
        foreach (char c in s)
            if (Array.IndexOf(invalid, c) < 0)
                buffer[len++] = c;
        return new string(buffer[..len]);
    }
}
