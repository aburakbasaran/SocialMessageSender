namespace SocialMediaMessaging.Core.Models;

/// <summary>
/// Platform kısıtlamaları
/// </summary>
public record PlatformConstraints
{
    /// <summary>
    /// Maksimum karakter sayısı
    /// </summary>
    public int MaxCharacters { get; init; } = 2000;
    
    /// <summary>
    /// Maksimum ek sayısı
    /// </summary>
    public int MaxAttachments { get; init; } = 10;
    
    /// <summary>
    /// Desteklenen ek türleri
    /// </summary>
    public List<string> SupportedAttachmentTypes { get; init; } = new();
    
    /// <summary>
    /// Zengin metin desteği
    /// </summary>
    public bool SupportsRichText { get; init; } = true;
    
    /// <summary>
    /// Markdown desteği
    /// </summary>
    public bool SupportsMarkdown { get; init; } = false;
    
    /// <summary>
    /// HTML desteği
    /// </summary>
    public bool SupportsHTML { get; init; } = false;
    
    /// <summary>
    /// Rate limit süresi
    /// </summary>
    public TimeSpan RateLimit { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Rate limit sayısı
    /// </summary>
    public int RateLimitCount { get; init; } = 30;
    
    /// <summary>
    /// Maksimum ek boyutu (byte cinsinden)
    /// </summary>
    public long MaxAttachmentSize { get; init; } = 20 * 1024 * 1024; // 20MB
    
    /// <summary>
    /// Mesaj zamanlaması desteği
    /// </summary>
    public bool SupportsScheduling { get; init; } = false;
    
    /// <summary>
    /// Mesaj düzenleme desteği
    /// </summary>
    public bool SupportsEditing { get; init; } = false;
    
    /// <summary>
    /// Mesaj silme desteği
    /// </summary>
    public bool SupportsDeletion { get; init; } = false;
    
    /// <summary>
    /// Emoji desteği
    /// </summary>
    public bool SupportsEmoji { get; init; } = true;
    
    /// <summary>
    /// Mention desteği
    /// </summary>
    public bool SupportsMentions { get; init; } = false;
    
    /// <summary>
    /// Hashtag desteği
    /// </summary>
    public bool SupportsHashtags { get; init; } = false;
    
    /// <summary>
    /// Thread desteği
    /// </summary>
    public bool SupportsThreads { get; init; } = false;
} 