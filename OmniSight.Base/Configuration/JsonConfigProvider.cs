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

        public static T LoadOrCreate<T>(string filePath, T defaults, string previousConfigPath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    string previousFile = Path.Combine(previousConfigPath, Path.GetFileName(filePath));
                    CreateConfig(filePath, previousFile, defaults);
                }

                return ReadConfig<T>(filePath);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is JsonException || error is DecoderFallbackException)
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)}: {error.Message}", error);
            }
        }

        public static T TryReadExisting<T>(string filePath, string previousConfigPath) where T : class
        {
            string path = File.Exists(filePath) ? filePath : Path.Combine(previousConfigPath, Path.GetFileName(filePath));
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
            var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath, Utf8), Settings);
            return value ?? throw new InvalidDataException("Expected a JSON object.");
        }

        private static void CreateConfig<T>(string filePath, string previousFile, T defaults)
        {
            string temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                if (File.Exists(previousFile))
                {
                    File.Copy(previousFile, temporaryPath);
                }
                else
                {
                    File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(defaults, Formatting.Indented, Settings), Utf8);
                }

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
