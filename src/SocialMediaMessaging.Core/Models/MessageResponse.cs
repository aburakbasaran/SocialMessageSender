using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.Core.Models;

/// <summary>
/// Mesaj gönderim yanıtı
/// </summary>
public record MessageResponse
{
    /// <summary>
    /// Mesaj benzersiz kimliği
    /// </summary>
    [Required]
    public required string MessageId { get; init; }
    
    /// <summary>
    /// Mesaj durumu
    /// </summary>
    public MessageStatus Status { get; init; } = MessageStatus.Pending;
    
    /// <summary>
    /// Platform sonuçları
    /// </summary>
    public Dictionary<string, PlatformResult> PlatformResults { get; init; } = new();
    
    /// <summary>
    /// Genel hatalar
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// Gönderim zamanı
    /// </summary>
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// İşlem süresi (milisaniye)
    /// </summary>
    public long ProcessingTimeMs { get; init; }
    
    /// <summary>
    /// Başarılı platform sayısı
    /// </summary>
    public int SuccessfulPlatforms => PlatformResults.Count(pr => pr.Value.Success);
    
    /// <summary>
    /// Başarısız platform sayısı
    /// </summary>
    public int FailedPlatforms => PlatformResults.Count(pr => !pr.Value.Success);
    
    /// <summary>
    /// Toplam platform sayısı
    /// </summary>
    public int TotalPlatforms => PlatformResults.Count;
    
    /// <summary>
    /// Tüm platformlarda başarılı mı?
    /// </summary>
    public bool IsCompletelySuccessful => 
        PlatformResults.Count > 0 && 
        PlatformResults.All(pr => pr.Value.Success);
    
    /// <summary>
    /// Hiç platform başarılı olmadı mı?
    /// </summary>
    public bool IsCompletelyFailed => 
        PlatformResults.Count > 0 && 
        PlatformResults.All(pr => !pr.Value.Success);
    
    /// <summary>
    /// Kısmi başarı var mı?
    /// </summary>
    public bool IsPartiallySuccessful => 
        PlatformResults.Count > 0 && 
        PlatformResults.Any(pr => pr.Value.Success) && 
        PlatformResults.Any(pr => !pr.Value.Success);
    
    /// <summary>
    /// Ortalama yanıt süresi
    /// </summary>
    public double AverageResponseTimeMs => 
        PlatformResults.Count > 0 ? 
        PlatformResults.Average(pr => pr.Value.ResponseTimeMs) : 0;
    
    /// <summary>
    /// Başarı oranı (0-1 arası)
    /// </summary>
    public double SuccessRate => 
        TotalPlatforms > 0 ? 
        (double)SuccessfulPlatforms / TotalPlatforms : 0;
    
    /// <summary>
    /// Başarılı sonuç oluşturur
    /// </summary>
    public static MessageResponse CreateSuccess(string messageId, Dictionary<string, PlatformResult>? platformResults = null)
    {
        return new MessageResponse
        {
            MessageId = messageId,
            Status = MessageStatus.Sent,
            PlatformResults = platformResults ?? new()
        };
    }
    
    /// <summary>
    /// Başarısız sonuç oluşturur
    /// </summary>
    public static MessageResponse CreateFailure(string messageId, List<string>? errors = null, Dictionary<string, PlatformResult>? platformResults = null)
    {
        return new MessageResponse
        {
            MessageId = messageId,
            Status = MessageStatus.Failed,
            Errors = errors ?? new(),
            PlatformResults = platformResults ?? new()
        };
    }
    
    /// <summary>
    /// Kısmi başarı sonucu oluşturur
    /// </summary>
    public static MessageResponse CreatePartialSuccess(string messageId, Dictionary<string, PlatformResult> platformResults, List<string>? errors = null)
    {
        return new MessageResponse
        {
            MessageId = messageId,
            Status = MessageStatus.PartialSuccess,
            PlatformResults = platformResults,
            Errors = errors ?? new()
        };
    }
} 