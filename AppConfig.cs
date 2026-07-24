using System.Runtime.InteropServices;
using System.Text.Json;

namespace EasyClipStash;

public class AppConfig
{
    public string SavePath { get; set; } = KnownFolders.Downloads;
    public ImageFormatKind ImageFormat { get; set; } = ImageFormatKind.Png;
    public string Hotkey { get; set; } = "Ctrl+Alt+V";
    public bool CopyMarkdownToClipboard { get; set; } = true;
    public string MarkdownUrlPrefix { get; set; } = "/assets/img";
    public string MarkdownTemplate { get; set; } = "![]({url})";
    public Lang Language { get; set; } = L.Default;
    public bool CheckUpdateOnStartup { get; set; } = true;

    // ── 파일 이름 규칙 (이미지·텍스트 각각 독립) ──
    public NamingConfig ImageNaming { get; set; } = new();
    public NamingConfig TextNaming { get; set; } = new();

    // ── 텍스트 저장 ──
    public string TextSavePath { get; set; } = KnownFolders.Downloads;  // 비우면 이미지와 같은 폴더(SavePath)
    public TextExtension TextExtension { get; set; } = TextExtension.Txt;

    /// <summary>텍스트를 실제로 저장할 폴더. TextSavePath가 비어 있으면 이미지 폴더를 쓴다.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string EffectiveTextFolder => string.IsNullOrWhiteSpace(TextSavePath) ? SavePath : TextSavePath;

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (loaded is not null)
                {
                    MigrateFlatNaming(loaded, json);
                    return loaded;
                }
            }
        }
        catch (JsonException)
        {
            // 손상된 config.json은 기본값으로 대체하되, 원본은 남겨둔다.
            var backup = ConfigPath + ".bak";
            File.Copy(ConfigPath, backup, overwrite: true);
        }

        var config = new AppConfig();
        config.Save();
        return config;
    }

    public void Save()
    {
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>
    /// 구버전 config(이름 규칙이 최상위에 평평하게 있던 형태)를 읽어 이미지·텍스트 규칙 양쪽에 적용한다.
    /// ImageNaming/TextNaming이 이미 있으면 아무것도 하지 않는다.
    /// </summary>
    private static void MigrateFlatNaming(AppConfig config, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty(nameof(ImageNaming), out _) || !root.TryGetProperty("NamingMode", out _))
            return;

        var legacy = JsonSerializer.Deserialize<NamingConfig>(root.GetRawText(), JsonOptions);
        if (legacy is null) return;

        config.ImageNaming = legacy;
        config.TextNaming = JsonSerializer.Deserialize<NamingConfig>(JsonSerializer.Serialize(legacy, JsonOptions), JsonOptions)!;
        config.Save(); // 새 형식으로 다시 기록
    }

    /// <summary>저장된 파일 경로에 대응하는 블로그용 마크다운 태그를 만든다.</summary>
    public string BuildMarkdown(string savedFilePath)
    {
        string url = MarkdownUrlPrefix.TrimEnd('/') + "/" + Path.GetFileName(savedFilePath);
        return MarkdownTemplate.Replace("{url}", url);
    }
}

/// <summary>Environment.SpecialFolder에 없는 알려진 폴더 경로 조회.</summary>
internal static class KnownFolders
{
    private static readonly Guid DownloadsId = new("374DE290-123F-4565-9164-39C4925E467B");

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(in Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    /// <summary>사용자의 실제 다운로드 폴더. (위치를 옮긴 경우에도 정확) 조회 실패 시 프로필\Downloads.</summary>
    public static string Downloads
    {
        get
        {
            if (SHGetKnownFolderPath(DownloadsId, 0, IntPtr.Zero, out IntPtr path) == 0)
            {
                try
                {
                    string? result = Marshal.PtrToStringUni(path);
                    if (!string.IsNullOrEmpty(result))
                        return result;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(path);
                }
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }
}
