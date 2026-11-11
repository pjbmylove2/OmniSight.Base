using System;
using System.Collections.Generic;
using OmniSight.Base.Configuration;
#if BASLER
using OmniSight.Base.Devices.Cameras;
#endif

namespace OmniSight.Base.Devices
{
    internal static class DeviceFactory
    {
        public static IReadOnlyList<IDevice> Create(CameraSettings settings)
        {
            if (settings.Mode == CameraMode.Disabled)
            {
                return Array.Empty<IDevice>();
            }

#if BASLER
            return new IDevice[] { new BaslerCameraAdapter(settings) };
#else
            throw new InvalidOperationException("Camera.json selects Basler. Build Debug-Basler or Release-Basler with pylon installed.");
#endif
        }
    }
}
