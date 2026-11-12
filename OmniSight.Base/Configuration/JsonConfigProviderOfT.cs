using System;
using System.IO;

namespace OmniSight.Base.Configuration
{
    /// <summary>Reads validated snapshots and atomically replaces a configuration file.</summary>
    public sealed class JsonConfigProvider<T> where T : class, new()
    {
        private readonly string _filePath;
        private readonly Action<T> _validate;
        private string _loadedJson;
        private string _snapshot;

        public JsonConfigProvider(string filePath, Action<T> validate = null)
        {
            _filePath = Path.GetFullPath(filePath);
            _validate = validate;
        }

        public T Load(string previousConfigPath = null)
        {
            lock (JsonConfigProvider.SaveLock)
            {
                // Keep Base's first-run and previous-layout migration behavior.
                T loaded = JsonConfigProvider.LoadOrCreate(_filePath, new T(), previousConfigPath, out string fileJson, _validate);
                string snapshot = JsonConfigProvider.Serialize(loaded);
                _loadedJson = fileJson;
                _snapshot = snapshot;
                return loaded;
            }
        }

        public T GetClone()
        {
            lock (JsonConfigProvider.SaveLock)
            {
                EnsureLoaded();
                return JsonConfigProvider.Deserialize<T>(_snapshot);
            }
        }

        public void ApplyAndSave(T edited)
        {
            if (edited == null)
            {
                throw new ArgumentNullException(nameof(edited));
            }

            lock (JsonConfigProvider.SaveLock)
            {
                EnsureLoaded();
                T candidate = JsonConfigProvider.Deserialize<T>(JsonConfigProvider.Serialize(edited));
                _validate?.Invoke(candidate);
                string json = JsonConfigProvider.Serialize(candidate);

                if (!string.Equals(JsonConfigProvider.ReadText(_filePath), _loadedJson, StringComparison.Ordinal))
                {
                    throw new IOException("The configuration changed on disk. Reload it before saving.");
                }

                if (string.Equals(json, _snapshot, StringComparison.Ordinal))
                {
                    return;
                }

                JsonConfigProvider.WriteFile(_filePath, json, replace: true);
                _loadedJson = json;
                _snapshot = json;
            }
        }

        private void EnsureLoaded()
        {
            if (_snapshot == null)
            {
                throw new InvalidOperationException("Load must be called before editing the configuration.");
            }
        }
    }
}
