using System.IO;

namespace OmniSight.Base.Configuration
{
    internal sealed class SystemSettings
    {
        public bool WriteDebugLog { get; set; } = true;
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
        public string Subtitle { get; set; } = "FA application starter";
    }
}
