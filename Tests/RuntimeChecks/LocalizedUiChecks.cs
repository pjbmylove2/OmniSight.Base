using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OmniSight.Base.Configuration;
using OmniSight.Base.Localization;
using OmniSight.Base.UI;
using OmniSight.Base.UI.Forms;
using OmniSight.Base.UI.Utils;

internal static class LocalizedUiChecks
{
    public static void Run(string root)
    {
        Check(FontManager.IsCustomFontLoaded && (FontManager.ActiveFontName.Contains("Nanum") || FontManager.ActiveFontName.Contains("나눔")), "embedded Nanum Gothic loaded (" + FontManager.ActiveFontName + ")");
        foreach (LanguageType language in Enum.GetValues(typeof(LanguageType)))
        {
            CheckSplash(root, language);
            CheckDialog(language);
        }

        CheckSplashFailure(root);
        CheckLanguageSwitch();
        LocalizationManager.ChangeLanguage(LanguageType.KOREAN);
        Check(MessageDialog.Show("Check", buttons: MessageBoxButtons.OKCancel, autoCloseSeconds: 1) == DialogResult.Cancel,
            "timed confirmation uses the safe negative response");
    }

    private static void CheckSplash(string root, LanguageType language)
    {
        string[] cancel = { "취소", "Cancel", "取消", "Anuluj" };
        string[] status = { "초기화 완료 (100%)", "Initialization complete (100%)", "初始化完成 (100%)", "Inicjalizacja zakończona (100%)" };
        string[] site = { "비전 검사 시스템", "VISION INSPECTION SYSTEM", "视觉检测系统", "SYSTEM INSPEKCJI WIZYJNEJ" };
        int index = (int)language;
        SplashBrandingChecks.RunSplash(root, "Language." + language, "{\"ProductName\":\"Configured product\"}", appRoot =>
        {
            File.WriteAllText(Path.Combine(appRoot, "SYSTEM", "SystemGeneral.json"), "{\"Language\":\"" + language + "\"}");
        }, (form, _) =>
        {
            var panel = form.Controls["pnlProgress"];
            var hero = form.Controls["pnlHero"];
            Check(panel.Controls["btnCancel"].Text == cancel[index] && panel.Controls["lblStatusCaption"].Text == status[index],
                "localized splash progress and cancel for " + language);
            Check(hero.Controls["lblSiteName"].Text == site[index] && hero.Controls["lblProductName"].Text == "Configured product",
                "localized defaults and configured branding for " + language);
            CheckCaptionsFit(form, language);
            Check(panel.Controls["lblStatus"].Font.Name == FontManager.ActiveFontName, "splash uses the selected font");
        });
    }

    private static void CheckSplashFailure(string root)
    {
        SplashBrandingChecks.RunSplash(root, "LanguageFailure", "{}", appRoot =>
        {
            string system = Path.Combine(appRoot, "SYSTEM");
            File.WriteAllText(Path.Combine(system, "SystemGeneral.json"), "{\"Language\":\"POLISH\"}");
            string camera = Path.Combine(appRoot, "DEVICE", "CAMERA");
            Directory.CreateDirectory(camera);
            File.WriteAllText(Path.Combine(camera, "Camera.json"), "{broken");
        }, (form, _) =>
        {
            var panel = form.Controls["pnlProgress"];
            Check(panel.Controls["lblStatusCaption"].Text == "Błąd inicjalizacji" &&
                  panel.Controls["btnCancel"].Text == "Zamknij" && panel.Controls["lblStatus"].Text.Contains("Camera.json"),
                "localized startup error retains the diagnostic file name");
            CheckCaptionFits(panel.Controls["btnCancel"], LanguageType.POLISH);
        }, expectFailure: true);
    }

    private static void CheckDialog(LanguageType language)
    {
        string[] severity = { "확인", "CONFIRMATION", "确认", "POTWIERDZENIE" };
        string[] yes = { "예(&Y)", "Yes(&Y)", "是(&Y)", "Tak(&Y)" };
        string[] no = { "아니오(&N)", "No(&N)", "否(&N)", "Nie(&N)" };
        int index = (int)language;
        LocalizationManager.ChangeLanguage(language);
        using (var dialog = new MessageDialogForm("Technical detail 42", null, MessageSeverity.Warning, MessageBoxButtons.YesNo, 0))
        {
            dialog.Shown += (sender, args) =>
            {
                try
                {
                    Check(dialog.Controls.Find("lblSeverity", true).Single().Text == severity[index], "confirmation severity for " + language);
                    Check(dialog.Controls.Find("lblMessage", true).Single().Text == "Technical detail 42", "caller message is preserved");
                    var yesButton = (Button)dialog.Controls.Find("btnYes", true).Single();
                    var noButton = (Button)dialog.Controls.Find("btnNo", true).Single();
                    Check(yesButton.Text == yes[index] && noButton.Text == no[index], "dialog buttons for " + language);
                    CheckCaptionsFit(dialog, language);
                    noButton.PerformClick();
                }
                finally
                {
                    if (dialog.DialogResult == DialogResult.None) { dialog.Close(); }
                }
            };
            Check(dialog.ShowDialog() == DialogResult.No, "negative response returns No");
        }
    }

    private static void CheckLanguageSwitch()
    {
        LocalizationManager.ChangeLanguage(LanguageType.KOREAN);
        using (var dialog = new MessageDialogForm())
        {
            var button = (Button)dialog.Controls.Find("btnOk", true).Single();
            Check(button.Text == "확인(&O)", "Korean designer caption");
            LocalizationManager.ChangeLanguage(LanguageType.ENGLISH);
            UiLocalization.Apply(dialog);
            Check(button.Text == "OK(&O)", "live captions can be reapplied");
            LocalizationManager.ChangeLanguage(LanguageType.KOREAN);
            UiLocalization.Apply(dialog);
            Check(button.Text == "확인(&O)", "original caption is restored");
        }
    }

    private static void CheckCaptionsFit(Control root, LanguageType language)
    {
        foreach (Control child in root.Controls)
        {
            if (child.Visible && (child is Label || child is Button))
            {
                CheckCaptionFits(child, language);
            }
            CheckCaptionsFit(child, language);
        }
    }

    private static void CheckCaptionFits(Control control, LanguageType language)
    {
        if (!string.IsNullOrWhiteSpace(control.Text))
        {
            Size preferred = control.GetPreferredSize(new Size(control.Width, 0));
            Check(preferred.Height <= control.Height + 2, "caption height " + language + " " + control.Name + " (" + preferred.Height + "/" + control.Height + ")");
            if (control is Button)
            {
                Check(TextRenderer.MeasureText(control.Text, control.Font).Width <= control.Width,
                    "button width " + language + " " + control.Name);
            }
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) { throw new InvalidOperationException("Failed: " + name); }
    }
}
