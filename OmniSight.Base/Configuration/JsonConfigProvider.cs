using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OmniSight.Base.Configuration
{
    internal static class JsonConfigProvider
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter { AllowIntegerValues = false } }
        };

        public static T LoadOrCreate<T>(string filePath, T defaults) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    WriteDefaults(filePath, defaults);
                }

                var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath, Utf8), Settings);
                return value ?? throw new InvalidDataException("Expected a JSON object.");
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is JsonException || error is DecoderFallbackException)
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)}: {error.Message}", error);
            }
        }

        private static void WriteDefaults<T>(string filePath, T defaults)
        {
            string temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(defaults, Formatting.Indented, Settings), Utf8);
                File.Move(temporaryPath, filePath);
            }
            catch (IOException) when (File.Exists(filePath))
            {
                // Another instance has already created the configuration.
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
