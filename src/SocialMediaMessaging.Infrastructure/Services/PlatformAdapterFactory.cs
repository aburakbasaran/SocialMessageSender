namespace SocialMediaMessaging.Infrastructure.Services;

/// <summary>
/// Platform adaptör fabrikası
/// </summary>
public class PlatformAdapterFactory : IPlatformAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlatformAdapterFactory> _logger;
    private readonly Dictionary<string, Type> _adapters;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public PlatformAdapterFactory(IServiceProvider serviceProvider, ILogger<PlatformAdapterFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _adapters = new Dictionary<string, Type>();
        
        RegisterBuiltInAdapters();
    }

    /// <summary>
    /// Platform adaptörü getirir
    /// </summary>
    public IPlatformAdapter? GetAdapter(string platformName)
    {
        if (string.IsNullOrWhiteSpace(platformName))
        {
            _logger.LogWarning("Platform adı boş");
            return null;
        }

        var normalizedName = platformName.ToLowerInvariant();
        
        if (!_adapters.TryGetValue(normalizedName, out var adapterType))
        {
            _logger.LogWarning("Desteklenmeyen platform: {Platform}", platformName);
            return null;
        }

        try
        {
            var adapter = (IPlatformAdapter?)_serviceProvider.GetService(adapterType);
            
            if (adapter == null)
            {
                _logger.LogError("Platform adaptörü oluşturulamadı: {Platform}", platformName);
                return null;
            }

            _logger.LogDebug("Platform adaptörü oluşturuldu: {Platform}", platformName);
            return adapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Platform adaptörü oluşturma hatası: {Platform}", platformName);
            return null;
        }
    }

    /// <summary>
    /// Tüm adaptörleri getirir
    /// </summary>
    public IEnumerable<IPlatformAdapter> GetAllAdapters()
    {
        var adapters = new List<IPlatformAdapter>();

        foreach (var adapterType in _adapters.Values)
        {
            try
            {
                var adapter = (IPlatformAdapter?)_serviceProvider.GetService(adapterType);
                if (adapter != null)
                {
                    adapters.Add(adapter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Adaptör oluşturma hatası: {AdapterType}", adapterType.Name);
            }
        }

        return adapters;
    }

    /// <summary>
    /// Aktif adaptörleri getirir
    /// </summary>
    public IEnumerable<IPlatformAdapter> GetActiveAdapters()
    {
        return GetAllAdapters().Where(adapter => adapter.IsEnabled);
    }

    /// <summary>
    /// Desteklenen platform adlarını getirir
    /// </summary>
    public IEnumerable<string> GetSupportedPlatforms()
    {
        return _adapters.Keys;
    }

    /// <summary>
    /// Platform destekleniyor mu?
    /// </summary>
    public bool IsPlatformSupported(string platformName)
    {
        if (string.IsNullOrWhiteSpace(platformName))
            return false;

        return _adapters.ContainsKey(platformName.ToLowerInvariant());
    }

    /// <summary>
    /// Platform aktif mi?
    /// </summary>
    public bool IsPlatformActive(string platformName)
    {
        var adapter = GetAdapter(platformName);
        return adapter?.IsEnabled ?? false;
    }

    /// <summary>
    /// Adaptör kaydeder
    /// </summary>
    public void RegisterAdapter(IPlatformAdapter adapter)
    {
        if (adapter == null)
        {
            throw new ArgumentNullException(nameof(adapter));
        }

        var platformName = adapter.PlatformName.ToLowerInvariant();
        _adapters[platformName] = adapter.GetType();
        
        _logger.LogInformation("Platform adaptörü kaydedildi: {Platform}", adapter.PlatformName);
    }

    /// <summary>
    /// Adaptör tipini kaydeder
    /// </summary>
    public void RegisterAdapter<T>(string platformName) where T : class, IPlatformAdapter
    {
        if (string.IsNullOrWhiteSpace(platformName))
        {
            throw new ArgumentException("Platform adı boş olamaz", nameof(platformName));
        }

        var normalizedName = platformName.ToLowerInvariant();
        _adapters[normalizedName] = typeof(T);
        
        _logger.LogInformation("Platform adaptör tipi kaydedildi: {Platform} -> {Type}", platformName, typeof(T).Name);
    }

    /// <summary>
    /// Adaptör kaydını kaldırır
    /// </summary>
    public bool UnregisterAdapter(string platformName)
    {
        if (string.IsNullOrWhiteSpace(platformName))
            return false;

        var normalizedName = platformName.ToLowerInvariant();
        
        if (_adapters.Remove(normalizedName))
        {
            _logger.LogInformation("Platform adaptörü kaydı kaldırıldı: {Platform}", platformName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Yerleşik adaptörleri kaydeder
    /// </summary>
    private void RegisterBuiltInAdapters()
    {
        // Bu metodda tüm built-in adapter'ları kaydediyoruz
        var adapterMappings = new Dictionary<string, Type>
        {
            { "telegram", typeof(Adapters.TelegramAdapter) },
            { "twitter", typeof(Adapters.TwitterAdapter) },
            { "discord", typeof(Adapters.DiscordAdapter) },
            // Diğer adapter'lar eklenecek
        };

        foreach (var mapping in adapterMappings)
        {
            _adapters[mapping.Key] = mapping.Value;
            _logger.LogDebug("Yerleşik adapter kaydedildi: {Platform} -> {Type}", 
                mapping.Key, mapping.Value.Name);
        }

        _logger.LogInformation("Toplam {Count} yerleşik adapter kaydedildi", _adapters.Count);
    }

    /// <summary>
    /// Platform capabilities bilgilerini getirir
    /// </summary>
    public Dictionary<string, object> GetPlatformCapabilities(string platformName)
    {
        var adapter = GetAdapter(platformName);
        if (adapter == null)
        {
            return new Dictionary<string, object>();
        }

        var capabilities = new Dictionary<string, object>
        {
            { "platformName", adapter.PlatformName },
            { "isEnabled", adapter.IsEnabled },
            { "constraints", adapter.Constraints },
            { "supportedMessageTypes", adapter.GetSupportedMessageTypes() },
            { "parameters", adapter.GetPlatformParameters() }
        };

        return capabilities;
    }

    /// <summary>
    /// Tüm platformların capabilities bilgilerini getirir
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> GetAllPlatformCapabilities()
    {
        var allCapabilities = new Dictionary<string, Dictionary<string, object>>();

        foreach (var platformName in GetSupportedPlatforms())
        {
            try
            {
                var capabilities = GetPlatformCapabilities(platformName);
                allCapabilities[platformName] = capabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Platform capabilities alınamadı: {Platform}", platformName);
                allCapabilities[platformName] = new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "isEnabled", false }
                };
            }
        }

        return allCapabilities;
    }

    /// <summary>
    /// Platform health check
    /// </summary>
    public async Task<Dictionary<string, bool>> PerformHealthCheckAsync()
    {
        var healthResults = new Dictionary<string, bool>();

        foreach (var platformName in GetSupportedPlatforms())
        {
            try
            {
                var adapter = GetAdapter(platformName);
                if (adapter != null && adapter.IsEnabled)
                {
                    var isHealthy = await adapter.TestConnectionAsync();
                    healthResults[platformName] = isHealthy;
                    
                    _logger.LogDebug("Platform health check: {Platform} = {Status}", 
                        platformName, isHealthy ? "Healthy" : "Unhealthy");
                }
                else
                {
                    healthResults[platformName] = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Platform health check hatası: {Platform}", platformName);
                healthResults[platformName] = false;
            }
        }

        return healthResults;
    }

    /// <summary>
    /// Aktif adaptör sayısını döner
    /// </summary>
    public int GetActiveAdapterCount()
    {
        return GetActiveAdapters().Count();
    }

    /// <summary>
    /// Toplam adaptör sayısını döner
    /// </summary>
    public int GetTotalAdapterCount()
    {
        return _adapters.Count;
    }
} 