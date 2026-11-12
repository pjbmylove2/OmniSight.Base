using System;
using System.IO;
using System.Text;

namespace OmniSight.Base.Logging
{
    /// <summary>
    /// 로그와 결과 CSV가 사용하는 공통 Unicode 파일 정책입니다.
    ///
    /// 모든 새 파일은 UTF-8 BOM으로 생성합니다. BOM이 있으면 메모장과 Excel 같은
    /// Windows 프로그램도 시스템 언어와 관계없이 UTF-8을 안정적으로 인식합니다.
    /// 기존 파일이 UTF-8이면 BOM만 보충하고, ANSI(CP949 등) 또는 여러 인코딩이
    /// 섞인 파일이면 원본을 보존 파일로 옮긴 후 새 UTF-8 파일을 시작합니다.
    /// 서로 다른 인코딩을 같은 파일에 이어 쓰면 어느 디코더로도 완전하게 읽을 수
    /// 없으므로, 기존 파일을 임의 변환하는 것보다 분리 보존하는 편이 안전합니다.
    /// </summary>
    internal static class UnicodeLogFile
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        // 잘못 만들어진 단독 surrogate 문자가 들어오더라도 로그 기록 스레드가 멈추지
        // 않게 replacement fallback을 사용합니다. 정상적인 모든 Unicode 문자는 손실 없이 기록됩니다.
        public static Encoding Utf8WithBom { get; } = new UTF8Encoding(true, false);

        /// <summary>
        /// append로 열기 전에 기존 파일을 검사합니다.
        /// 반환값이 null이 아니면 비 UTF-8 원본을 해당 경로에 보존했다는 뜻입니다.
        /// </summary>
        public static string PrepareForAppend(string path, bool trustUtf8Bom = false)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                return null;
            }

            bool hasUtf8Bom = HasUtf8Bom(path);
            if (hasUtf8Bom && trustUtf8Bom)
            {
                return null;
            }
            if (IsValidUtf8(path))
            {
                if (!hasUtf8Bom)
                {
                    PrependUtf8Bom(path);
                }

                return null;
            }

            string archivePath = CreateLegacyArchivePath(path);
            File.Move(path, archivePath);
            return archivePath;
        }

        private static bool HasUtf8Bom(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return stream.Length >= 3 &&
                       stream.ReadByte() == 0xEF &&
                       stream.ReadByte() == 0xBB &&
                       stream.ReadByte() == 0xBF;
            }
        }

        private static bool IsValidUtf8(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, StrictUtf8, false, 4096))
                {
                    var buffer = new char[4096];
                    while (reader.Read(buffer, 0, buffer.Length) > 0)
                    {
                        // 전체 파일을 끝까지 읽어야 후반에 섞인 ANSI 바이트도 검출됩니다.
                    }
                }

                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static void PrependUtf8Bom(string path)
        {
            string temporaryPath = path + ".utf8-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    byte[] preamble = Utf8WithBom.GetPreamble();
                    destination.Write(preamble, 0, preamble.Length);
                    source.CopyTo(destination);
                    destination.Flush(true);
                }

                // 같은 디렉터리에서 원자적으로 교체하므로 도중에 중단되어도 원본 또는
                // 완성된 UTF-8 파일 중 하나가 남습니다.
                File.Replace(temporaryPath, path, null, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private static string CreateLegacyArchivePath(string path)
        {
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            string candidate = Path.Combine(
                directory,
                fileName + ".legacy-encoding-" + timestamp + extension);

            int suffix = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(
                    directory,
                    fileName + ".legacy-encoding-" + timestamp + "-" + suffix + extension);
                suffix++;
            }

            return candidate;
        }
    }

}
