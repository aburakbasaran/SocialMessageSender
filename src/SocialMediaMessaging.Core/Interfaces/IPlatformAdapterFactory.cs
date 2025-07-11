namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Platform adaptör fabrikası arayüzü
/// </summary>
public interface IPlatformAdapterFactory
{
    /// <summary>
    /// Platform adaptörü getirir
    /// </summary>
    /// <param name="platformName">Platform adı</param>
    /// <returns>Platform adaptörü</returns>
    IPlatformAdapter? GetAdapter(string platformName);
    
    /// <summary>
    /// Tüm adaptörleri getirir
    /// </summary>
    /// <returns>Tüm adaptörler</returns>
    IEnumerable<IPlatformAdapter> GetAllAdapters();
    
    /// <summary>
    /// Aktif adaptörleri getirir
    /// </summary>
    /// <returns>Aktif adaptörler</returns>
    IEnumerable<IPlatformAdapter> GetActiveAdapters();
    
    /// <summary>
    /// Desteklenen platform adlarını getirir
    /// </summary>
    /// <returns>Platform adları</returns>
    IEnumerable<string> GetSupportedPlatforms();
    
    /// <summary>
    /// Platform destekleniyor mu?
    /// </summary>
    /// <param name="platformName">Platform adı</param>
    /// <returns>Destekleniyor mu?</returns>
    bool IsPlatformSupported(string platformName);
    
    /// <summary>
    /// Platform aktif mi?
    /// </summary>
    /// <param name="platformName">Platform adı</param>
    /// <returns>Aktif mi?</returns>
    bool IsPlatformActive(string platformName);
    
    /// <summary>
    /// Adaptör kaydeder
    /// </summary>
    /// <param name="adapter">Adaptör</param>
    void RegisterAdapter(IPlatformAdapter adapter);
    
    /// <summary>
    /// Adaptör kaydını kaldırır
    /// </summary>
    /// <param name="platformName">Platform adı</param>
    /// <returns>Kaldırıldı mı?</returns>
    bool UnregisterAdapter(string platformName);
} 