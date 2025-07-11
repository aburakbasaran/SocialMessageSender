using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.Core.Models;

/// <summary>
/// Mesaj gönderme isteği
/// </summary>
public record MessageRequest
{
    /// <summary>
    /// Mesaj içeriği
    /// </summary>
    [Required]
    public required string Content { get; init; }
    
    /// <summary>
    /// Mesaj tipi
    /// </summary>
    public MessageType Type { get; init; } = MessageType.Text;
    
    /// <summary>
    /// Hedef platformlar
    /// </summary>
    [Required]
    public required List<string> Platforms { get; init; } = new();
    
    /// <summary>
    /// Platform özel veriler
    /// </summary>
    public Dictionary<string, object> PlatformSpecificData { get; init; } = new();
    
    /// <summary>
    /// Mesaj ekleri
    /// </summary>
    public List<Attachment> Attachments { get; init; } = new();
    
    /// <summary>
    /// Mesaj önceliği
    /// </summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    
    /// <summary>
    /// Zamanlanmış gönderim zamanı
    /// </summary>
    public DateTime? ScheduledAt { get; init; }
    
    /// <summary>
    /// Yeniden deneme özelliği aktif mi?
    /// </summary>
    public bool EnableRetry { get; init; } = true;
    
    /// <summary>
    /// Maksimum yeniden deneme sayısı
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>
    /// İstek benzersiz kimliği
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Kullanıcı kimliği
    /// </summary>
    public string? UserId { get; init; }
    
    /// <summary>
    /// Mesaj etiketleri
    /// </summary>
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
    
    /// <summary>
    /// Mesaj başlığı (destekleyen platformlar için)
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// Mesaj alt başlığı
    /// </summary>
    public string? Subtitle { get; init; }
    
    /// <summary>
    /// Callback URL (gönderim sonucu için)
    /// </summary>
    public string? CallbackUrl { get; init; }
    
    /// <summary>
    /// Mesaj grubunda mi? (thread/reply)
    /// </summary>
    public bool IsThreadMessage { get; init; } = false;
    
    /// <summary>
    /// Ana mesaj ID'si (thread için)
    /// </summary>
    public string? ParentMessageId { get; init; }
    
    /// <summary>
    /// Mesaj geçerli mi?
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Content) &&
        Platforms.Count > 0 &&
        Platforms.All(p => !string.IsNullOrWhiteSpace(p)) &&
        (ScheduledAt == null || ScheduledAt > DateTime.UtcNow) &&
        MaxRetryAttempts >= 0 &&
        MaxRetryAttempts <= 10;
} 