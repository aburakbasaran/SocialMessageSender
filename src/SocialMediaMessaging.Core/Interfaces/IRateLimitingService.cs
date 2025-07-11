namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Rate limiting servisi arayüzü
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// İstek izinli mi?
    /// </summary>
    /// <param name="platform">Platform adı</param>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <returns>İzin verilen mi?</returns>
    Task<bool> IsAllowedAsync(string platform, string identifier);
    
    /// <summary>
    /// İsteği kaydeder
    /// </summary>
    /// <param name="platform">Platform adı</param>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <returns>Task</returns>
    Task RecordRequestAsync(string platform, string identifier);
    
    /// <summary>
    /// Rate limit durumunu getirir
    /// </summary>
    /// <param name="platform">Platform adı</param>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <returns>Rate limit durumu</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string platform, string identifier);
    
    /// <summary>
    /// Rate limit'i sıfırlar
    /// </summary>
    /// <param name="platform">Platform adı</param>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <returns>Task</returns>
    Task ResetRateLimitAsync(string platform, string identifier);
    
    /// <summary>
    /// Tüm rate limit'leri temizler
    /// </summary>
    /// <returns>Task</returns>
    Task ClearAllRateLimitsAsync();
    
    /// <summary>
    /// Rate limit yapılandırmasını günceller
    /// </summary>
    /// <param name="platform">Platform adı</param>
    /// <param name="limit">Limit</param>
    /// <param name="window">Zaman penceresi</param>
    /// <returns>Task</returns>
    Task UpdateRateLimitConfigurationAsync(string platform, int limit, TimeSpan window);

    /// <summary>
    /// İstek işlenebilir mi kontrolü (MessageService için)
    /// </summary>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İşlenebilir mi?</returns>
    Task<bool> CanProcessAsync(string identifier, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// İstek sayısını artır (MessageService için)
    /// </summary>
    /// <param name="identifier">Benzersiz kimlik</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Task</returns>
    Task IncrementAsync(string identifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit durumu
/// </summary>
public record RateLimitStatus
{
    /// <summary>
    /// İzin verilen mi?
    /// </summary>
    public bool IsAllowed { get; init; }
    
    /// <summary>
    /// Kalan istek sayısı
    /// </summary>
    public int RemainingRequests { get; init; }
    
    /// <summary>
    /// Toplam limit
    /// </summary>
    public int TotalLimit { get; init; }
    
    /// <summary>
    /// Sıfırlanma zamanı
    /// </summary>
    public DateTime ResetTime { get; init; }
    
    /// <summary>
    /// Zaman penceresi
    /// </summary>
    public TimeSpan Window { get; init; }
    
    /// <summary>
    /// Son istek zamanı
    /// </summary>
    public DateTime LastRequestTime { get; init; }
    
    /// <summary>
    /// Platform adı
    /// </summary>
    public string Platform { get; init; } = string.Empty;
    
    /// <summary>
    /// Kimlik
    /// </summary>
    public string Identifier { get; init; } = string.Empty;
} 