using System.Globalization;

namespace EasyClipStash;

public enum Lang { Korean, English }

/// <summary>
/// 앱의 모든 UI 문자열을 한/영으로 제공한다. <see cref="Current"/>를 바꾸면 이후 조회되는 문자열이 그 언어로 나온다.
/// 매개변수가 있는 문자열은 메서드, 고정 문자열은 프로퍼티로 둔다.
/// </summary>
public static class L
{
    public static Lang Current { get; set; } = DetectDefault();

    private static bool Ko => Current == Lang.Korean;

    /// <summary>시스템 UI 언어가 한국어면 한국어, 아니면 영어를 기본값으로.</summary>
    public static Lang DetectDefault()
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko" ? Lang.Korean : Lang.English;

    // 언어 선택 드롭다운에 표시할 이름 (자국어 표기라 번역하지 않는다)
    public static string DisplayName(Lang lang) => lang == Lang.Korean ? "한국어" : "English";

    // ── 트레이 메뉴 ──
    public static string SaveClipboard(string hotkey)
        => Ko ? $"클립보드 저장 ({hotkey})" : $"Save clipboard ({hotkey})";
    public static string OpenLastFile => Ko ? "마지막 파일 열기" : "Open last file";
    public static string OpenImageFolder => Ko ? "이미지 저장 폴더 열기" : "Open image folder";
    public static string OpenTextFolder => Ko ? "텍스트 저장 폴더 열기" : "Open text folder";
    public static string CopyMarkdownAuto => Ko ? "마크다운 자동 복사" : "Auto-copy markdown";
    public static string RunAtStartup => Ko ? "시작 프로그램 등록" : "Run at startup";
    public static string Settings => Ko ? "설정..." : "Settings...";
    public static string Exit => Ko ? "종료" : "Exit";

    // ── 알림 ──
    public static string HotkeyErrorTitle => Ko ? "단축키 오류" : "Hotkey error";
    public static string SaveFailedTitle => Ko ? "저장 실패" : "Save failed";
    public static string NothingToSave
        => Ko ? "클립보드에 저장할 이미지나 텍스트가 없습니다." : "No image or text in the clipboard to save.";
    public static string SavedTo(string dir, string file)
        => Ko ? $"{dir}에 {file} 저장 완료." : $"Saved {file} to {dir}.";
    public static string MarkdownCopied => Ko ? "마크다운 태그 복사 완료." : "Markdown tag copied.";
    public static string FolderMissingTitle => Ko ? "폴더 없음" : "Folder not found";
    public static string FolderMissing(string path)
        => Ko ? $"저장 폴더가 없습니다: {path}" : $"Save folder not found: {path}";

    // ── 실행/공통 ──
    public static string AlreadyRunning
        => Ko ? "EasyClipStash가 이미 실행 중입니다.\n트레이 아이콘을 확인하세요."
              : "EasyClipStash is already running.\nCheck the tray icon.";

    // ── 설정 창 ──
    public static string SettingsWindowTitle => Ko ? "EasyClipStash 설정" : "EasyClipStash Settings";
    public static string GroupGeneral => Ko ? "기본" : "General";
    public static string GroupNaming => Ko ? "파일 이름" : "File name";
    public static string GroupImage => Ko ? "이미지 설정" : "Image";
    public static string GroupText => Ko ? "텍스트 설정" : "Text";
    public static string SaveFolderLabel => Ko ? "이미지 저장 위치" : "Image save location";
    public static string HotkeyLabel => Ko ? "단축키" : "Hotkey";
    public static string HotkeyHint
        => Ko ? "칸을 클릭한 뒤 원하는 키 조합을 누르세요 (예: Ctrl+Alt+V)"
              : "Click the box, then press your key combination (e.g. Ctrl+Alt+V)";
    public static string UrlPrefixLabel => Ko ? "URL 경로" : "URL path";
    public static string TemplateLabel => Ko ? "템플릿" : "Template";
    public static string LanguageLabel => Ko ? "언어" : "Language";
    public static string CopyMarkdownCheck
        => Ko ? "저장 후 마크다운 태그를 클립보드에 복사" : "Copy markdown tag to clipboard after saving";
    public static string Browse => Ko ? "찾아보기..." : "Browse...";
    public static string Save => Ko ? "저장" : "Save";
    public static string Cancel => Ko ? "취소" : "Cancel";
    public static string FolderPickerDescription
        => Ko ? "이미지를 저장할 폴더 선택" : "Choose a folder to save images";
    public static string TextFolderPickerDescription
        => Ko ? "텍스트를 저장할 폴더 선택" : "Choose a folder to save text";
    public static string SettingsDialogTitle => Ko ? "설정" : "Settings";
    public static string EnterSaveFolder => Ko ? "이미지 저장 위치를 입력하세요." : "Please enter an image save location.";
    public static string FolderNotExistConfirm(string path)
        => Ko ? $"폴더가 존재하지 않습니다:\n{path}\n\n그래도 저장할까요?"
              : $"The folder does not exist:\n{path}\n\nSave anyway?";
    public static string ResetButton => Ko ? "설정 초기화" : "Reset settings";
    public static string ResetConfirm
        => Ko ? "모든 설정을 기본값으로 되돌릴까요?\n('저장'을 눌러야 실제로 적용됩니다.)"
              : "Reset all settings to their defaults?\n(Takes effect when you click 'Save'.)";

    // ── 파일 이름 규칙 ──
    public static string NamingModeLabel => Ko ? "저장 방식" : "Naming mode";
    public static string StartNumberLabel => Ko ? "시작 번호" : "Start number";
    public static string PaddingLabel => Ko ? "자릿수 맞추기" : "Zero-padding";
    public static string DateStyleLabel => Ko ? "날짜 형식" : "Date format";
    public static string TimeStyleLabel => Ko ? "시간 형식" : "Time format";
    public static string PrefixLabel => Ko ? "이름 앞에 붙일 말" : "Name prefix";
    public static string SuffixLabel => Ko ? "이름 뒤에 붙일 말" : "Name suffix";
    public static string ImagePreview(string name)
        => Ko ? $"이미지 저장 예시: {name}" : $"Image save example: {name}";
    public static string TextPreview(string name)
        => Ko ? $"텍스트 저장 예시: {name}" : $"Text save example: {name}";
    public static string TabImage => Ko ? "이미지" : "Image";
    public static string TabText => Ko ? "텍스트" : "Text";
    public static string MarkdownSection => Ko ? "마크다운" : "Markdown";

    // ── 업데이트 ──
    public static string UpdateSection => Ko ? "업데이트" : "Update";
    public static string CurrentVersionLabel => Ko ? "현재 버전" : "Current version";
    public static string CheckUpdate => Ko ? "업데이트 확인" : "Check for updates";
    public static string UpdateNow => Ko ? "지금 업데이트" : "Update now";
    public static string AutoCheckUpdate
        => Ko ? "시작할 때 자동으로 업데이트 확인" : "Check for updates at startup";
    public static string UpdateChecking => Ko ? "확인 중…" : "Checking…";
    public static string UpToDate => Ko ? "최신 버전입니다." : "You are up to date.";
    public static string UpdateAvailable(Version v)
        => Ko ? $"새 버전 {v} 이(가) 있습니다." : $"Version {v} is available.";
    public static string UpdateDownloading(int percent)
        => Ko ? $"내려받는 중… {percent}%" : $"Downloading… {percent}%";
    public static string UpdateVerifying => Ko ? "무결성 확인 중…" : "Verifying…";
    public static string UpdateRestarting => Ko ? "업데이트를 적용하려면 재시작합니다." : "Restarting to apply the update.";
    public static string UpdateFailed(string reason)
        => Ko ? $"업데이트 실패: {reason}" : $"Update failed: {reason}";
    public static string UpdateNotSupported
        => Ko ? "개발 빌드에서는 자동 업데이트를 쓸 수 없습니다." : "Auto-update is unavailable in development builds.";
    public static string UpdateChecksumMismatch
        => Ko ? "내려받은 파일의 체크섬이 일치하지 않습니다." : "Checksum of the downloaded file does not match.";
    public static string UpdateChecksumMissing
        => Ko ? "이 릴리스에는 체크섬이 없어 자동 설치할 수 없습니다. 직접 내려받아 주세요."
              : "This release has no checksum, so it cannot be installed automatically. Please download it manually.";
    public static string UpdateExeMissing
        => Ko ? "내려받은 파일에서 실행 파일을 찾지 못했습니다." : "Executable not found in the downloaded package.";

    // ── 텍스트 저장 ──
    public static string TextFolderLabel => Ko ? "텍스트 저장 위치" : "Text save location";
    public static string TextFolderHint
        => Ko ? "비워두면 이미지와 같은 폴더에 저장합니다." : "Leave blank to use the image save location.";
    public static string TextExtLabel => Ko ? "텍스트 확장자" : "Text extension";
    public static string ImageFormatLabel => Ko ? "이미지 형식" : "Image format";
    public static string ImageFormatName(ImageFormatKind k) => FileNamer.Extension(k);

    public static string None => Ko ? "없음" : "None";

    // ── enum 표시 이름 ──
    public static string NamingModeName(NamingMode m) => m switch
    {
        NamingMode.Number => Ko ? "번호 매기기" : "Numbering",
        NamingMode.DateTime => Ko ? "날짜_시간" : "Date_Time",
        NamingMode.DateDaily => Ko ? "날짜_순번" : "Date_Number",
        _ => m.ToString(),
    };

    public static string PadName(PadWidth p) => p switch
    {
        PadWidth.None => None,
        PadWidth.Two => Ko ? "2자리 (01)" : "2 digits (01)",
        PadWidth.Three => Ko ? "3자리 (001)" : "3 digits (001)",
        PadWidth.Four => Ko ? "4자리 (0001)" : "4 digits (0001)",
        _ => p.ToString(),
    };

    /// <summary>날짜 형식은 언어와 무관하게 실제 예시(20001017 등)로 보여준다.</summary>
    public static string DateStyleName(DateStyle s) => FileNamer.Sample.ToString(FileNamer.DateFormat(s));

    public static string TimeStyleName(TimeStyle t)
        => t == TimeStyle.None ? None : FileNamer.Sample.ToString(FileNamer.TimeFormat(t));

    public static string TextExtName(TextExtension e) => FileNamer.Extension(e);

    // ── 단축키 파싱 오류 ──
    public static string CannotParseHotkey(string token)
        => Ko ? $"단축키를 해석할 수 없습니다: \"{token}\" (예: Ctrl+Alt+V)"
              : $"Cannot parse hotkey: \"{token}\" (e.g. Ctrl+Alt+V)";
    public static string NoMainKey(string hotkeyText)
        => Ko ? $"단축키에 일반 키가 없습니다: \"{hotkeyText}\" (예: Ctrl+Alt+V)"
              : $"Hotkey has no main key: \"{hotkeyText}\" (e.g. Ctrl+Alt+V)";
    public static string HotkeyRegisterFailed(string hotkeyText)
        => Ko ? $"단축키 등록 실패: {hotkeyText} (다른 프로그램이 이미 사용 중일 수 있습니다)"
              : $"Failed to register hotkey: {hotkeyText} (another program may already be using it)";

    // ── 이미지 저장 ──
    public static string SaveFolderNotFound(string folder)
        => Ko ? $"저장 폴더가 없습니다: {folder}" : $"Save folder not found: {folder}";
}
