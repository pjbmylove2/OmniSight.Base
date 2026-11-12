using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using OmniSight.Base.Configuration;

namespace OmniSight.Base.Localization
{
    /// <summary>
    /// 번역 카탈로그는 부팅 때 한 번 읽습니다. 프레임 로그에서는 불변 사전만 조회합니다.
    /// 선택 언어를 변경한 후 열린 화면에 UiLocalization을 다시 적용할 수 있습니다.
    /// </summary>
    public static class LocalizationManager
    {
        private static readonly object Sync = new object();
        private static volatile Dictionary<string, CatalogEntry> _entries =
            new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        private static volatile Dictionary<string, CatalogEntry> _sourceEntries =
            new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        private static volatile LanguageType _language = LanguageType.KOREAN;
        private static volatile Dictionary<string, string> _captionSources = new Dictionary<string, string>(StringComparer.Ordinal);

        public static event EventHandler LanguageChanged;

        public static LanguageType CurrentLanguage
        {
            get
            {
                lock (Sync)
                {
                    return _language;
                }
            }
        }

        public static string SourceSha256 { get; private set; } = string.Empty;
        public static int EntryCount => _entries.Count;
        public static string GetSource(string caption)
        {
            string normalized = caption?.Replace("\r\n", "\n");
            return normalized != null && _captionSources.TryGetValue(normalized, out string source) ? source : caption;
        }
        public static string GetKorean(string key)
        {
            return key != null && _entries.TryGetValue(key, out CatalogEntry entry) ? entry.Ko : null;
        }

        public static string GetEnumText(Enum value)
        {
            return value == null ? string.Empty : Get("Enum." + value.GetType().Name + "." + value, value.ToString());
        }

        /// <summary>Exact source caption lookup. Does not format or rewrite data embedded in text.</summary>
        public static string TranslateSource(string source)
        {
            if (string.IsNullOrEmpty(source)) { return source; }
            return _sourceEntries.TryGetValue(source.Replace("\r\n", "\n"), out CatalogEntry entry)
                ? entry.GetText(_language, source) : source;
        }

        public static void Initialize()
        {
            using (Stream stream = typeof(LocalizationManager).Assembly.GetManifestResourceStream("OmniSight.Base.Localization.catalog.json"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("The embedded localization catalog is missing."), Encoding.UTF8))
            {
                LoadCatalog(reader.ReadToEnd(), LanguageType.KOREAN);
            }
        }

        public static bool TryInitialize(string catalogPath, LanguageType language, out string error)
        {
            error = null;
            try
            {
                LoadCatalog(File.ReadAllText(catalogPath, Encoding.UTF8), language);
                return true;
            }
            catch (Exception exception) when (exception is IOException ||
                exception is UnauthorizedAccessException || exception is JsonException ||
                exception is InvalidDataException || exception is ArgumentException || exception is FormatException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void LoadCatalog(string json, LanguageType language)
        {
            if (!Enum.IsDefined(typeof(LanguageType), language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            CatalogDocument document = JsonConvert.DeserializeObject<CatalogDocument>(json);
            if (document == null || document.SchemaVersion != 1 || document.Entries == null)
            {
                throw new InvalidDataException("지원하지 않는 번역 카탈로그 형식입니다.");
            }

            var entries = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
            foreach (CatalogEntry entry in document.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) ||
                    string.IsNullOrWhiteSpace(entry.Ko))
                {
                    throw new InvalidDataException("번역 카탈로그에 Key 또는 한국어가 비었습니다.");
                }

                if (entries.ContainsKey(entry.Key))
                {
                    throw new InvalidDataException("번역 Key가 중복됐습니다: " + entry.Key);
                }
                ValidateTranslation(entry.Ko, entry.En);
                ValidateTranslation(entry.Ko, entry.Zh);
                ValidateTranslation(entry.Ko, entry.Pl);
                entries.Add(entry.Key, entry);
            }
            if (entries.Count == 0)
            {
                throw new InvalidDataException("번역 카탈로그가 비었습니다.");
            }

            var sourceEntries = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
            var captionSources = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in document.Entries)
            {
                if (!sourceEntries.ContainsKey(entry.Ko)) { sourceEntries.Add(entry.Ko, entry); }
                foreach (string caption in new[] { entry.Ko, entry.En, entry.Zh, entry.Pl })
                {
                    if (!string.IsNullOrEmpty(caption) && !captionSources.ContainsKey(caption)) { captionSources.Add(caption, entry.Ko); }
                }
            }
            lock (Sync)
            {
                _entries = entries;
                _sourceEntries = sourceEntries;
                _captionSources = captionSources;
                _language = language;
                SourceSha256 = document.SourceSha256 ?? string.Empty;
            }
        }

        public static void ChangeLanguage(LanguageType language)
        {
            if (!Enum.IsDefined(typeof(LanguageType), language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            EventHandler changed;
            lock (Sync)
            {
                if (_language == language)
                {
                    return;
                }

                _language = language;
                changed = LanguageChanged;
            }
            changed?.Invoke(null, EventArgs.Empty);
        }

        private static readonly Regex Placeholder = new Regex(@"(?<!\{)\{\d+(?:,\s*-?\d+)?(?::[^{}]*)?\}(?!\})", RegexOptions.CultureInvariant);

        public static void ValidateTranslation(string korean, string translation)
        {
            if (string.IsNullOrWhiteSpace(translation)) { return; }
            if (korean.Contains("*.") && korean.Contains("|"))
            {
                string[] originalFilter = korean.Split('|');
                string[] translatedFilter = translation.Split('|');
                if (originalFilter.Length != translatedFilter.Length || originalFilter.Length % 2 != 0 ||
                    originalFilter.Where((v, i) => i % 2 == 1).Where((v, i) => v != translatedFilter[i * 2 + 1]).Any())
                {
                    throw new InvalidDataException("파일 필터의 확장자 또는 구분자를 번역하면 안 됩니다.");
                }
            }
            string[] source = Placeholder.Matches(korean).Cast<Match>().Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            string[] target = Placeholder.Matches(translation).Cast<Match>().Select(m => m.Value).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            if (!source.SequenceEqual(target)) { throw new InvalidDataException("번역의 자리표시자 또는 숫자 형식이 한국어와 다릅니다."); }
            if (source.Length > 0)
            {
                // Validate braces/index/alignment once at load time, never in the frame loop.
                string.Format(System.Globalization.CultureInfo.InvariantCulture, korean, new object[256]);
                string.Format(System.Globalization.CultureInfo.InvariantCulture, translation, new object[256]);
            }
        }

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("번역 Key가 비었습니다.", nameof(key));
            }

            // Dictionaries are built privately and published once; no hot-path locks or I/O.
            CatalogEntry entry;
            return _entries.TryGetValue(key, out entry) ? entry.GetText(_language, fallback) : fallback;
        }

        public static bool TryGetBoundText(string key, string binding, out string text)
        {
            lock (Sync)
            {
                CatalogEntry entry;
                if (_entries.TryGetValue(key, out entry) && entry.Binding == binding)
                {
                    text = entry.GetText(_language, null);
                    return !string.IsNullOrEmpty(text);
                }
            }
            text = null;
            return false;
        }

        private sealed class CatalogDocument
        {
            public int SchemaVersion { get; set; }
            public string SourceSha256 { get; set; }
            public List<CatalogEntry> Entries { get; set; }
        }

        private sealed class CatalogEntry
        {
            public string Key { get; set; }
            public string Ko { get; set; }
            public string En { get; set; }
            public string Zh { get; set; }
            public string Pl { get; set; }
            public string Binding { get; set; }

            public string GetText(LanguageType language, string fallback)
            {
                string selected;
                switch (language)
                {
                    case LanguageType.ENGLISH: selected = En; break;
                    case LanguageType.CHINESE: selected = Zh; break;
                    case LanguageType.POLISH: selected = Pl; break;
                    default: selected = Ko; break;
                }
                return !string.IsNullOrWhiteSpace(selected) ? selected :
                    !string.IsNullOrWhiteSpace(Ko) ? Ko : fallback;
            }
        }
    }
}
