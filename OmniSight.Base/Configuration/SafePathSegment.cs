using System;
using System.IO;
using System.Linq;

namespace OmniSight.Base.Configuration
{
    /// <summary>Windows 파일 및 폴더 이름에 안전한 경로 조각을 만듭니다.</summary>
    internal static class SafePathSegment
    {
        public static string Sanitize(string value, string fallback = "UNKNOWN")
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string result = new string((value ?? string.Empty).Select(c =>
                char.IsControl(c) || invalid.Contains(c) || "<>:\"/\\|?*".IndexOf(c) >= 0 ? '_' : c).ToArray()).Trim().TrimEnd('.', ' ');
            if (result.Length == 0 || result == "." || result == "..")
            {
                return fallback;
            }
            string device = result.Split('.')[0].ToUpperInvariant();
            if (device == "CON" || device == "PRN" || device == "AUX" || device == "NUL" ||
                (device.Length == 4 && (device.StartsWith("COM") || device.StartsWith("LPT")) &&
                 "123456789¹²³".IndexOf(device[3]) >= 0))
            {
                result = "_" + result;
            }
            return result;
        }
    }
}
