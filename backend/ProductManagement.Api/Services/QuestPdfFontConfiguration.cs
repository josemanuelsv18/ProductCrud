using QuestPDF.Drawing;

namespace ProductManagement.Api.Services;

public static class QuestPdfFontConfiguration
{
    private const string RegisteredFontFamily = "ProductManagementPdfFont";
    private static bool _configured;

    public static string DefaultFontFamily { get; private set; } = "DejaVu Sans";

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        QuestPDF.Settings.UseEnvironmentFonts = true;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;

        foreach (var fontPath in GetCandidateFontFiles())
        {
            if (!File.Exists(fontPath))
            {
                continue;
            }

            using var stream = File.OpenRead(fontPath);
            FontManager.RegisterFontWithCustomName(RegisteredFontFamily, stream);
            DefaultFontFamily = RegisteredFontFamily;
            _configured = true;
            return;
        }

        _configured = true;
    }

    private static IEnumerable<string> GetCandidateFontFiles()
    {
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";
        yield return @"C:\Windows\Fonts\arial.ttf";
    }
}
