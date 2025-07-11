namespace SocialMediaMessaging.Core.Enums;

/// <summary>
/// Mesaj tipini belirtir
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Düz metin mesajı
    /// </summary>
    Text = 1,
    
    /// <summary>
    /// Zengin metin mesajı (HTML, Markdown)
    /// </summary>
    RichText = 2,
    
    /// <summary>
    /// Resim içeren mesaj
    /// </summary>
    Image = 3,
    
    /// <summary>
    /// Video içeren mesaj
    /// </summary>
    Video = 4,
    
    /// <summary>
    /// Döküman içeren mesaj
    /// </summary>
    Document = 5
} 