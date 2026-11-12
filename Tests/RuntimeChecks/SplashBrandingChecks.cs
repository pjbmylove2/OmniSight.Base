using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using OmniSight.Base.UI;

internal static class SplashBrandingChecks
{
    public static void Run(string root)
    {
        CheckFullBranding(root);
        CheckLogoFallback(root);
        CheckExecutableRelativeLogo(root);
    }

    private static void CheckFullBranding(string root)
    {
        const string relativeLogo = "Config/SplashLogo.png";
        RunSplash(root, "FullBranding", @"{
  ""Title"": ""Old title"",
  ""ProductName"": ""Branded product"",
  ""Subtitle"": ""Test subtitle"",
  ""SiteName"": ""Test site"",
  ""EquipmentName"": ""Test equipment"",
  ""CompanyName"": ""Test company"",
  ""FooterText"": ""Test footer"",
  ""LogoText"": ""FB"",
  ""LogoPath"": ""Config/SplashLogo.png"",
  ""AccentColor"": ""#2752F3"",
  ""BrandPanelColor"": ""#1E43E2""
}", appRoot => WriteLogo(Path.Combine(appRoot, relativeLogo)), (form, appRoot) =>
        {
            var hero = (StartupBrandPanel)form.Controls["pnlHero"];
            Check(hero.Controls["lblProductName"].Text == "Branded product" &&
                  hero.Controls["lblSubtitle"].Text == "Test subtitle" &&
                  hero.Controls["lblSiteName"].Text == "Test site" &&
                  hero.Controls["lblEquipmentName"].Text == "Test equipment" &&
                  hero.Controls["lblCompanyName"].Text == "Test company" &&
                  form.Controls["pnlProgress"].Controls["lblFooter"].Text == "Test footer",
                  "all text fields are visible and ProductName takes precedence over Title");
            Check(hero.PrimaryColor.ToArgb() == Color.FromArgb(0x27, 0x52, 0xF3).ToArgb() &&
                  hero.SecondaryColor.ToArgb() == Color.FromArgb(0x1E, 0x43, 0xE2).ToArgb(), "both panel colors");
            var picture = (PictureBox)hero.Controls["picLogo"];
            Check(picture.Image is Bitmap logo && logo.GetPixel(1, 1).ToArgb() == Color.Magenta.ToArgb(),
                "data-root relative image is displayed in place of LogoText");
            using (File.Open(Path.Combine(appRoot, relativeLogo), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // A visible logo must not keep its source file open.
            }
        });
    }

    private static void CheckLogoFallback(string root)
    {
        RunSplash(root, "FallbackBranding",
            "{\"Title\":\"Legacy title\",\"LogoText\":\"FB\",\"LogoPath\":\"SYSTEM/Invalid.png\",\"AccentColor\":\"invalid\"}",
            appRoot => File.WriteAllText(Path.Combine(appRoot, "SYSTEM", "Invalid.png"), "not an image"),
            (form, appRoot) =>
            {
                var hero = (StartupBrandPanel)form.Controls["pnlHero"];
                var picture = (PictureBox)hero.Controls["picLogo"];
                Check(hero.Controls["lblProductName"].Text == "Legacy title", "legacy Title remains usable");
                Check(hero.PrimaryColor.ToArgb() == Color.FromArgb(0, 192, 0).ToArgb(), "invalid color uses designer default");
                Check(picture.Image == null && picture.AccessibleName.StartsWith("FB"), "invalid image uses LogoText");
                using (var screenshot = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                {
                    form.DrawToBitmap(screenshot, form.ClientRectangle);
                    Point lens = form.PointToClient(picture.PointToScreen(
                        new Point(picture.ClientSize.Width * 40 / 112, picture.ClientSize.Height * 67 / 112)));
                    Check(screenshot.GetPixel(lens.X, lens.Y).ToArgb() == Color.FromArgb(30, 191, 93).ToArgb(),
                        "text-logo badge is painted");
                }
            });
    }

    private static void CheckExecutableRelativeLogo(string root)
    {
        string fileName = "SplashBrandingCheck-" + Guid.NewGuid().ToString("N") + ".png";
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        try
        {
            WriteLogo(path);
            RunSplash(root, "ExecutableBranding", "{\"LogoPath\":\"" + fileName + "\"}", appRoot => { },
                (form, appRoot) =>
                {
                    var picture = (PictureBox)form.Controls["pnlHero"].Controls["picLogo"];
                    Check(picture.Image is Bitmap logo && logo.GetPixel(1, 1).ToArgb() == Color.Magenta.ToArgb(),
                        "executable-relative logo path is supported");
                });
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteLogo(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var image = new Bitmap(4, 4))
        {
            image.SetPixel(1, 1, Color.Magenta);
            image.Save(path, ImageFormat.Png);
        }
    }

    private static void RunSplash(string root, string appId, string json, Action<string> prepare, Action<StartupForm, string> inspect)
    {
        string storage = Path.Combine(root, "branding");
        string appRoot = Path.Combine(storage, "Company", appId);
        string system = Path.Combine(appRoot, "SYSTEM");
        Directory.CreateDirectory(system);
        File.WriteAllText(Path.Combine(system, "SplashBranding.json"), json);
        prepare(appRoot);

        Assembly assembly = typeof(StartupForm).Assembly;
        Type pathsType = assembly.GetType("OmniSight.Base.Configuration.PathManager", throwOnError: true);
        object paths = Activator.CreateInstance(pathsType, BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { appId, "Company", storage, Path.Combine(root, "previous") }, null);
        Type bootstrapperType = assembly.GetType("OmniSight.Base.Application.ApplicationBootstrapper", throwOnError: true);
        using (var bootstrapper = (IDisposable)Activator.CreateInstance(bootstrapperType, paths))
        using (var form = (StartupForm)Activator.CreateInstance(typeof(StartupForm), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { bootstrapper }, null))
        using (var timer = new Timer { Interval = 20 })
        {
            bool inspected = false;
            Exception failure = null;
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            timer.Tick += (sender, args) =>
            {
                var panel = form.Controls["pnlProgress"];
                if (((StartupProgressBar)panel.Controls["progressBoot"]).Value == 100)
                {
                    timer.Stop();
                    try
                    {
                        inspect(form, appRoot);
                        inspected = true;
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                }
                else if (DateTime.UtcNow > deadline || form.Text.EndsWith(" Startup failed", StringComparison.Ordinal))
                {
                    timer.Stop();
                    form.Close();
                }
            };

            timer.Start();
            DialogResult result = form.ShowDialog();
            timer.Stop();
            if (failure != null)
            {
                throw new InvalidOperationException("Splash inspection failed for " + appId, failure);
            }
            Check(result == DialogResult.OK && inspected, "configured splash completed for " + appId);
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }
}
