namespace SocialMediaMessaging.Core.Models;

/// <summary>
/// Platform gönderim sonucu
/// </summary>
public record PlatformResult
{
    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Platform mesaj ID'si
    /// </summary>
    public string? PlatformMessageId { get; init; }
    
    /// <summary>
    /// Platform adı
    /// </summary>
    [Required]
    public required string PlatformName { get; init; }
    
    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Detaylı hata bilgisi
    /// </summary>
    public string? ErrorDetails { get; init; }
    
    /// <summary>
    /// Hata kodu
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// Gönderim zamanı
    /// </summary>
    public DateTime? SentAt { get; init; }
    
    /// <summary>
    /// Deneme sayısı
    /// </summary>
    public int AttemptNumber { get; init; } = 1;
    
    /// <summary>
    /// Yanıt süresi (milisaniye)
    /// </summary>
    public long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Platform özel veriler
    /// </summary>
    public Dictionary<string, object> PlatformData { get; init; } = new();
    
    /// <summary>
    /// Mesaj URL'i (varsa)
    /// </summary>
    public string? MessageUrl { get; init; }
    
    /// <summary>
    /// Başarılı sonuç oluşturur
    /// </summary>
    public static PlatformResult CreateSuccess(string platformName, string? platformMessageId = null, string? messageUrl = null)
    {
        return new PlatformResult
        {
            Success = true,
            PlatformName = platformName,
            PlatformMessageId = platformMessageId,
            MessageUrl = messageUrl,
            SentAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Başarısız sonuç oluşturur
    /// </summary>
    public static PlatformResult CreateFailure(string platformName, string error, string? errorCode = null, string? errorDetails = null)
    {
        return new PlatformResult
        {
            Success = false,
            PlatformName = platformName,
            Error = error,
            ErrorCode = errorCode,
            ErrorDetails = errorDetails,
            SentAt = DateTime.UtcNow
        };
    }
} 