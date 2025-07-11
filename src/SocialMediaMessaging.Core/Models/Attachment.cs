using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.Core.Models;

/// <summary>
/// Mesaj eki
/// </summary>
public record Attachment
{
    /// <summary>
    /// Dosya adı
    /// </summary>
    [Required]
    public required string FileName { get; init; }
    
    /// <summary>
    /// İçerik tipi (MIME type)
    /// </summary>
    [Required]
    public required string ContentType { get; init; }
    
    /// <summary>
    /// Dosya verisi (byte array)
    /// </summary>
    public byte[]? Data { get; init; }
    
    /// <summary>
    /// Dosya URL'i (Data yerine kullanılabilir)
    /// </summary>
    public string? Url { get; init; }
    
    /// <summary>
    /// Dosya boyutu (byte cinsinden)
    /// </summary>
    public long Size { get; init; }
    
    /// <summary>
    /// Dosya açıklaması
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Thumbnail URL'i (resim/video için)
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Attachment'ın geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool IsValid => 
        !string.IsNullOrWhiteSpace(FileName) && 
        !string.IsNullOrWhiteSpace(ContentType) && 
        (Data?.Length > 0 || !string.IsNullOrWhiteSpace(Url));
} 