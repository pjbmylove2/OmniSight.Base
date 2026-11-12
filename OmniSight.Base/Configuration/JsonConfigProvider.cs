using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OmniSight.Base.Configuration
{
    internal static class JsonConfigProvider
    {
        internal static readonly object SaveLock = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter { AllowIntegerValues = false } }
        };

        public static T LoadOrCreate<T>(string filePath, T defaults, string previousConfigPath) where T : class =>
            LoadOrCreate(filePath, defaults, previousConfigPath, out _);

        internal static T LoadOrCreate<T>(string filePath, T defaults, string previousConfigPath, out string json, Action<T> validate = null) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    string previousFile = PreviousFile(filePath, previousConfigPath);
                    CreateConfig(filePath, previousFile, defaults, validate);
                }

                json = ReadText(filePath);
                T loaded = Deserialize<T>(json);
                validate?.Invoke(loaded);
                return loaded;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is JsonException || error is DecoderFallbackException)
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)}: {error.Message}", error);
            }
        }

        public static T TryReadExisting<T>(string filePath, string previousConfigPath) where T : class
        {
            string path = File.Exists(filePath) ? filePath : PreviousFile(filePath, previousConfigPath);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return ReadConfig<T>(path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is InvalidDataException || error is JsonException || error is DecoderFallbackException)
            {
                // The boot sequence reads the configuration again and reports the full error.
                return null;
            }
        }

        private static T ReadConfig<T>(string filePath) where T : class
        {
            return Deserialize<T>(ReadText(filePath));
        }

        internal static T Deserialize<T>(string json) where T : class
        {
            var value = JsonConvert.DeserializeObject<T>(json, Settings);
            return value ?? throw new InvalidDataException("Expected a JSON object.");
        }

        internal static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, Formatting.Indented, Settings);

        internal static string ReadText(string filePath) => File.ReadAllText(filePath, Utf8);

        internal static string PreviousFile(string filePath, string previousConfigPath) =>
            previousConfigPath == null ? null : Path.Combine(previousConfigPath, Path.GetFileName(filePath));

        private static void CreateConfig<T>(string filePath, string previousFile, T defaults, Action<T> validate) where T : class
        {
            try
            {
                string json = File.Exists(previousFile) ? ReadText(previousFile) : Serialize(defaults);
                T candidate = Deserialize<T>(json);
                validate?.Invoke(candidate);
                WriteFile(filePath, json, replace: false);
            }
            catch (IOException) when (File.Exists(filePath))
            {
                // Another instance has already created the configuration.
            }
        }

        internal static void WriteFile(string filePath, string json, bool replace)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            string temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, Utf8))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (replace)
                {
                    File.Replace(temporaryPath, filePath, filePath + ".bak");
                }
                else
                {
                    File.Move(temporaryPath, filePath);
                }
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    Trace.TraceWarning("Could not clean up temporary configuration: {0}", error.Message);
                }
            }
        }
    }
}
