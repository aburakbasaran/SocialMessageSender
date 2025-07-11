using SocialMediaMessaging.Core.Models;
using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Platform adaptörü arayüzü
/// </summary>
public interface IPlatformAdapter
{
    /// <summary>
    /// Platform adı
    /// </summary>
    string PlatformName { get; }
    
    /// <summary>
    /// Platform kısıtlamaları
    /// </summary>
    PlatformConstraints Constraints { get; }
    
    /// <summary>
    /// Platform aktif mi?
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Mesaj gönderir
    /// </summary>
    /// <param name="message">Gönderilecek mesaj</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Platform sonucu</returns>
    Task<PlatformResult> SendMessageAsync(MessageRequest message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mesajı doğrular
    /// </summary>
    /// <param name="message">Doğrulanacak mesaj</param>
    /// <returns>Geçerli mi?</returns>
    Task<bool> ValidateMessageAsync(MessageRequest message);
    
    /// <summary>
    /// İçeriği platforma göre dönüştürür
    /// </summary>
    /// <param name="content">Dönüştürülecek içerik</param>
    /// <param name="type">Mesaj tipi</param>
    /// <returns>Dönüştürülmüş içerik</returns>
    string TransformContent(string content, MessageType type);
    
    /// <summary>
    /// Platform konfigürasyonu geçerli mi?
    /// </summary>
    /// <returns>Geçerli mi?</returns>
    Task<bool> IsConfigurationValidAsync();
    
    /// <summary>
    /// Platform bağlantısını test eder
    /// </summary>
    /// <returns>Test sonucu</returns>
    Task<bool> TestConnectionAsync();
    
    /// <summary>
    /// Platform rate limit'ini kontrol eder
    /// </summary>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <returns>İzin verilen mi?</returns>
    Task<bool> CheckRateLimitAsync(string identifier);
    
    /// <summary>
    /// Desteklenen mesaj tiplerini döner
    /// </summary>
    /// <returns>Desteklenen tipler</returns>
    List<MessageType> GetSupportedMessageTypes();
    
    /// <summary>
    /// Platform özel parametreleri döner
    /// </summary>
    /// <returns>Platform parametreleri</returns>
    Dictionary<string, object> GetPlatformParameters();
} 