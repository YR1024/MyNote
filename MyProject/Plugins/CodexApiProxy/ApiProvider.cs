using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CodexApiProxy
{
    public class ApiProvider : INotifyPropertyChanged
    {
        private string _name;
        private string _apiUrl;
        private string _apiKey;
        private string _modelName;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string ApiUrl
        {
            get => _apiUrl;
            set { _apiUrl = value; OnPropertyChanged(nameof(ApiUrl)); }
        }

        public string ApiKey
        {
            get => _apiKey;
            set { _apiKey = value; OnPropertyChanged(nameof(ApiKey)); }
        }

        public string ModelName
        {
            get => _modelName;
            set { _modelName = value; OnPropertyChanged(nameof(ModelName)); }
        }

        public override string ToString() => Name ?? "(未命名)";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ProviderManager
    {
        private readonly string _filePath;
        private List<ApiProvider> _providers = new List<ApiProvider>();

        public IReadOnlyList<ApiProvider> Providers => _providers;

        public string ActiveProviderId { get; set; }

        public ApiProvider ActiveProvider =>
            _providers.FirstOrDefault(p => p.Id == ActiveProviderId) ?? _providers.FirstOrDefault();

        public ProviderManager()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "CodexApiProxy");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "providers.json");
        }

        public void Load()
        {
            if (!File.Exists(_filePath)) return;
            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<ProviderData>(json);
                if (data?.Providers != null)
                    _providers = data.Providers;
                ActiveProviderId = data?.ActiveProviderId;
            }
            catch
            {
                _providers = new List<ApiProvider>();
            }
        }

        public void Save()
        {
            var data = new ProviderData
            {
                Providers = _providers,
                ActiveProviderId = ActiveProviderId
            };
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Add(ApiProvider provider)
        {
            _providers.Add(provider);
            Save();
        }

        public void Update(ApiProvider provider)
        {
            var existing = _providers.FirstOrDefault(p => p.Id == provider.Id);
            if (existing != null)
            {
                var idx = _providers.IndexOf(existing);
                _providers[idx] = provider;
            }
            Save();
        }

        public void Remove(string providerId)
        {
            _providers.RemoveAll(p => p.Id == providerId);
            if (ActiveProviderId == providerId)
                ActiveProviderId = _providers.FirstOrDefault()?.Id;
            Save();
        }

        public void SetActive(string providerId)
        {
            ActiveProviderId = providerId;
            Save();
        }

        public ApiProvider GetById(string id)
        {
            return _providers.FirstOrDefault(p => p.Id == id);
        }

        private class ProviderData
        {
            public List<ApiProvider> Providers { get; set; }
            public string ActiveProviderId { get; set; }
        }
    }
}
