using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace EasyClipStash;

/// <summary>발견된 새 버전 정보.</summary>
public sealed record UpdateInfo(Version Version, string Tag, string ZipUrl, string Sha256Url, long Size);

/// <summary>
/// GitHub 릴리스를 확인하고 앱을 스스로 교체한다.
///
/// 교체 방식: Windows는 실행 중인 exe의 삭제는 막지만 이름 변경은 허용한다.
/// 그래서 현재 exe를 .old로 밀어두고 그 자리에 새 exe를 놓은 뒤 재시작하면,
/// 별도 업데이터 프로그램 없이 자기 자신을 갱신할 수 있다. (.old는 다음 실행 때 지운다)
/// </summary>
public static class Updater
{
    private const string Owner = "JeongJongMun";
    private const string Repo = "EasyClipStash";
    private const string LatestReleaseApi = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

    /// <summary>내려받기를 허용할 주소. 응답에 담긴 임의 URL을 그대로 따라가지 않기 위한 방어.</summary>
    private const string AllowedAssetPrefix = $"https://github.com/{Owner}/{Repo}/releases/download/";

    private const string OldSuffix = ".old";

    public static Version CurrentVersion
        => Assembly.GetEntryAssembly()?.GetName().Version is { } v ? new Version(v.Major, v.Minor, v.Build) : new Version(0, 0, 0);

    /// <summary>
    /// 단일 파일로 배포된 빌드에서만 자기 교체가 가능하다.
    /// 개발 빌드는 exe와 dll이 분리돼 있어 exe만 바꾸면 깨진다.
    /// </summary>
    public static bool IsSupported => string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        // GitHub API는 User-Agent가 없으면 403을 준다.
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Repo}/{CurrentVersion}");
        client.Timeout = TimeSpan.FromMinutes(10);
        return client;
    }

    /// <summary>최신 릴리스를 조회한다. 현재 버전보다 높을 때만 값을 돌려준다.</summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken token = default)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(LatestReleaseApi, token);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = doc.RootElement;

        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!TryParseTag(tag, out Version latest) || latest <= CurrentVersion)
            return null;

        string zipUrl = "", shaUrl = "";
        long size = 0;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            string url = asset.GetProperty("browser_download_url").GetString() ?? "";
            if (!url.StartsWith(AllowedAssetPrefix, StringComparison.Ordinal))
                continue;   // 예상 저장소 밖의 주소는 무시

            if (name.EndsWith(".zip.sha256", StringComparison.OrdinalIgnoreCase))
                shaUrl = url;
            else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                zipUrl = url;
                size = asset.GetProperty("size").GetInt64();
            }
        }

        return zipUrl.Length == 0 ? null : new UpdateInfo(latest, tag, zipUrl, shaUrl, size);
    }

    /// <summary>"v1.2.0" 형태의 태그를 버전으로 바꾼다.</summary>
    private static bool TryParseTag(string tag, out Version version)
    {
        version = new Version(0, 0, 0);
        string text = tag.TrimStart('v', 'V');
        if (!Version.TryParse(text, out var parsed)) return false;
        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    /// <summary>
    /// 새 버전을 내려받아 검증한 뒤 현재 exe를 교체하고 재시작한다.
    /// 성공하면 이 메서드는 돌아오지 않고 앱이 종료된다.
    /// </summary>
    public static async Task DownloadAndApplyAsync(UpdateInfo info, IProgress<int>? progress, CancellationToken token = default)
    {
        if (!IsSupported)
            throw new InvalidOperationException(L.UpdateNotSupported);

        string workDir = Path.Combine(Path.GetTempPath(), "EasyClipStash_update_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(workDir);
        try
        {
            // 체크섬이 없으면 무결성을 확인할 수 없으므로 설치하지 않는다(fail-closed).
            if (info.Sha256Url.Length == 0)
                throw new InvalidOperationException(L.UpdateChecksumMissing);

            string zipPath = Path.Combine(workDir, "update.zip");
            await DownloadAsync(info.ZipUrl, zipPath, info.Size, progress, token);
            await VerifyAsync(zipPath, info.Sha256Url, token);

            string newExe = ExtractExe(zipPath, workDir);
            SwapAndRestart(newExe);
        }
        catch
        {
            TryDelete(workDir);
            throw;
        }
    }

    private static async Task DownloadAsync(string url, string destination, long expectedSize, IProgress<int>? progress, CancellationToken token)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();

        long total = response.Content.Headers.ContentLength ?? expectedSize;
        await using var source = await response.Content.ReadAsStreamAsync(token);
        await using var target = File.Create(destination);

        var buffer = new byte[81920];
        long done = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, token)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), token);
            done += read;
            if (total > 0)
                progress?.Report((int)(done * 100 / total));
        }
    }

    /// <summary>배포된 SHA256과 내려받은 파일의 해시를 대조한다.</summary>
    private static async Task VerifyAsync(string filePath, string sha256Url, CancellationToken token)
    {
        using var client = CreateClient();
        string expected = (await client.GetStringAsync(sha256Url, token)).Trim().ToLowerInvariant();

        await using var stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, token);
        string actual = Convert.ToHexString(hash).ToLowerInvariant();

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException(L.UpdateChecksumMismatch);
    }

    private static string ExtractExe(string zipPath, string workDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("EasyClipStash.exe", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(L.UpdateExeMissing);

        string extracted = Path.Combine(workDir, "EasyClipStash.exe");
        entry.ExtractToFile(extracted, overwrite: true);
        return extracted;
    }

    /// <summary>현재 exe를 .old로 밀어내고 새 exe를 그 자리에 놓은 뒤 새 프로세스를 띄운다.</summary>
    private static void SwapAndRestart(string newExe)
    {
        string currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException(L.UpdateNotSupported);
        string oldExe = currentExe + OldSuffix;

        TryDeleteFile(oldExe);          // 지난 업데이트 잔재가 남아 있을 수 있다
        File.Move(currentExe, oldExe);  // 실행 중이어도 이름 변경은 허용된다
        try
        {
            File.Copy(newExe, currentExe);
        }
        catch
        {
            File.Move(oldExe, currentExe);   // 실패하면 원래대로 되돌린다
            throw;
        }

        // 새 인스턴스를 띄우기 전에 단일 실행 잠금을 먼저 푼다.
        // Application.Exit()는 종료를 시작만 하므로, 잠금을 쥔 채로 새 인스턴스를 띄우면
        // 새 인스턴스가 "이미 실행 중"으로 튕겨 나가 결국 아무것도 남지 않는다.
        Program.ReleaseInstanceLock();

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(currentExe)
        {
            UseShellExecute = true,
            Arguments = Program.AfterUpdateArgument,   // 혹시 늦게 풀리더라도 새 인스턴스가 기다려준다
        });
        Application.Exit();
    }

    /// <summary>앱 시작 시 지난 업데이트가 남긴 .old 파일을 정리한다.</summary>
    public static void CleanupPreviousUpdate()
    {
        if (Environment.ProcessPath is { } exe)
            TryDeleteFile(exe + OldSuffix);
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { /* 이전 프로세스가 아직 종료 전이면 다음 실행 때 지운다 */ }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDelete(string directory)
    {
        try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
