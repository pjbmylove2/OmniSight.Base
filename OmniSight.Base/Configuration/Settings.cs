using System.IO;

namespace OmniSight.Base.Configuration
{
    internal sealed class SystemSettings
    {
        public bool WriteDebugLog { get; set; } = true;
        public LanguageType Language { get; set; } = LanguageType.KOREAN;
    }

    internal enum CameraMode
    {
        Disabled,
        Basler
    }

    internal enum CameraTrigger
    {
        Continuous,
        Software,
        HardwareLine1
    }

    internal sealed class CameraSettings
    {
        public CameraMode Mode { get; set; } = CameraMode.Disabled;
        public string SerialNumber { get; set; } = "";
        public CameraTrigger Trigger { get; set; } = CameraTrigger.Continuous;

        public void Validate()
        {
            if (Mode == CameraMode.Basler && string.IsNullOrWhiteSpace(SerialNumber))
            {
                throw new InvalidDataException("Camera.json: a serial number is required in Basler mode.");
            }
        }
    }

    internal sealed class SplashBranding
    {
        public string Title { get; set; } = "";
        public string ProductName { get; set; }
        public string Subtitle { get; set; }
        public string SiteName { get; set; }
        public string EquipmentName { get; set; }
        public string CompanyName { get; set; }
        public string FooterText { get; set; }
        public string LogoText { get; set; }
        public string LogoPath { get; set; }
        public string AccentColor { get; set; }
        public string BrandPanelColor { get; set; }
    }
}
