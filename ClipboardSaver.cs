using System.Drawing.Imaging;

namespace ClipStash;

public enum SavedKind { Image, Text }

public readonly record struct SaveResult(string Path, SavedKind Kind);

public static class ClipboardSaver
{
    /// <summary>
    /// 클립보드 내용을 저장한다. 이미지가 있으면 이미지(PNG), 없고 텍스트가 있으면 텍스트로 저장한다.
    /// 저장할 게 없으면 null.
    /// </summary>
    public static SaveResult? Save(AppConfig config)
    {
        if (Clipboard.ContainsImage())
        {
            using var image = Clipboard.GetImage();
            if (image is not null)
            {
                string folder = config.SavePath;
                EnsureFolder(folder);
                string path = FileNamer.BuildNext(config.ImageNaming, folder, FileNamer.Extension(config.ImageFormat), DateTime.Now);
                SaveImage(image, path, config.ImageFormat);
                return new SaveResult(path, SavedKind.Image);
            }
        }

        if (Clipboard.ContainsText())
        {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                string folder = config.EffectiveTextFolder;
                EnsureFolder(folder);
                string path = FileNamer.BuildNext(config.TextNaming, folder, FileNamer.Extension(config.TextExtension), DateTime.Now);
                File.WriteAllText(path, text); // .NET 기본 UTF-8(BOM 없음)
                return new SaveResult(path, SavedKind.Text);
            }
        }

        return null;
    }

    /// <summary>선택한 형식으로 이미지를 저장한다.</summary>
    private static void SaveImage(Image image, string path, ImageFormatKind kind)
    {
        if (kind != ImageFormatKind.Jpg)
        {
            image.Save(path, ImageFormat.Png);
            return;
        }

        // JPEG는 투명도를 지원하지 않는다. 알파가 있는 이미지를 그대로 저장하면 투명 부분이 검게 나오므로
        // 흰 배경 위에 합성한 뒤 저장한다. 화질은 기본값(75)보다 높은 90으로 고정.
        using var flattened = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(flattened))
        {
            g.Clear(Color.White);
            g.DrawImage(image, 0, 0, image.Width, image.Height);
        }

        var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
        flattened.Save(path, encoder, parameters);
    }

    private static void EnsureFolder(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(L.SaveFolderNotFound(folder));
    }
}
