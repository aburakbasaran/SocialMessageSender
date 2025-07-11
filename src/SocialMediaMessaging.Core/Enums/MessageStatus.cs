namespace SocialMediaMessaging.Core.Enums;

/// <summary>
/// Mesaj durumu
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Beklemede
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Gönderildi
    /// </summary>
    Sent = 2,
    
    /// <summary>
    /// Başarısız
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Yeniden denenecek
    /// </summary>
    Retry = 4,
    
    /// <summary>
    /// Kısmen başarılı
    /// </summary>
    PartialSuccess = 5,
    
    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 6,
    
    /// <summary>
    /// Kısmen başarısız
    /// </summary>
    PartiallyFailed = 7,
    
    /// <summary>
    /// Zamanlanmış
    /// </summary>
    Scheduled = 8
} 