namespace SocialMediaMessaging.Infrastructure.Configuration;

/// <summary>
/// Platform konfigürasyonu
/// </summary>
public class PlatformConfiguration
{
    /// <summary>
    /// Platform aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maksimum karakter sayısı
    /// </summary>
    public int MaxCharacters { get; set; } = 2000;
    
    /// <summary>
    /// Rate limit süresi
    /// </summary>
    public TimeSpan RateLimit { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Rate limit sayısı
    /// </summary>
    public int RateLimitCount { get; set; } = 30;
    
    /// <summary>
    /// Bağlantı zaman aşımı
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Yeniden deneme sayısı
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Yeniden deneme gecikmesi
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// Telegram konfigürasyonu
/// </summary>
public class TelegramConfiguration : PlatformConfiguration
{
    /// <summary>
    /// Bot token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Varsayılan sohbet kimliği
    /// </summary>
    public string? DefaultChatId { get; set; }
    
    /// <summary>
    /// Parse mode
    /// </summary>
    public string ParseMode { get; set; } = "HTML";
    
    /// <summary>
    /// Bildirim sesini kapatır
    /// </summary>
    public bool DisableNotification { get; set; } = false;
    
    /// <summary>
    /// Web page önizlemesini kapatır
    /// </summary>
    public bool DisableWebPagePreview { get; set; } = false;
}

/// <summary>
/// Twitter konfigürasyonu
/// </summary>
public class TwitterConfiguration : PlatformConfiguration
{
    /// <summary>
    /// API anahtarı
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API gizli anahtarı
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Erişim token'ı
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Erişim token gizli anahtarı
    /// </summary>
    public string AccessTokenSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Bearer token
    /// </summary>
    public string? BearerToken { get; set; }
    
    /// <summary>
    /// Otomatik hashtag ekleme
    /// </summary>
    public bool AutoAddHashtags { get; set; } = false;
    
    /// <summary>
    /// Varsayılan hashtag'ler
    /// </summary>
    public List<string> DefaultHashtags { get; set; } = new();
}

/// <summary>
/// Discord konfigürasyonu
/// </summary>
public class DiscordConfiguration : PlatformConfiguration
{
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Bot token (bot API için)
    /// </summary>
    public string? BotToken { get; set; }
    
    /// <summary>
    /// Varsayılan kanal kimliği
    /// </summary>
    public string? DefaultChannelId { get; set; }
    
    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// TTS kullanımı
    /// </summary>
    public bool UseTTS { get; set; } = false;
    
    /// <summary>
    /// Embed kullanımı
    /// </summary>
    public bool UseEmbed { get; set; } = true;
    
    /// <summary>
    /// Embed rengi
    /// </summary>
    public string EmbedColor { get; set; } = "#0099ff";
}

/// <summary>
/// Slack konfigürasyonu
/// </summary>
public class SlackConfiguration : PlatformConfiguration
{
    /// <summary>
    /// Bot token
    /// </summary>
    public string BotToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Varsayılan kanal
    /// </summary>
    public string? DefaultChannel { get; set; }
    
    /// <summary>
    /// Bot kullanıcı adı
    /// </summary>
    public string? BotUsername { get; set; }
    
    /// <summary>
    /// Bot icon emoji
    /// </summary>
    public string? BotIconEmoji { get; set; }
    
    /// <summary>
    /// Bot icon URL
    /// </summary>
    public string? BotIconUrl { get; set; }
    
    /// <summary>
    /// Markdown kullanımı
    /// </summary>
    public bool UseMarkdown { get; set; } = true;
    
    /// <summary>
    /// Link isimlerini göster
    /// </summary>
    public bool UnfurlLinks { get; set; } = true;
    
    /// <summary>
    /// Medya isimlerini göster
    /// </summary>
    public bool UnfurlMedia { get; set; } = true;
}

/// <summary>
/// WhatsApp konfigürasyonu
/// </summary>
public class WhatsAppConfiguration : PlatformConfiguration
{
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Telefon numarası kimliği
    /// </summary>
    public string PhoneNumberId { get; set; } = string.Empty;
    
    /// <summary>
    /// WhatsApp Business hesap kimliği
    /// </summary>
    public string BusinessAccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook gizli anahtarı
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// API versiyonu
    /// </summary>
    public string ApiVersion { get; set; } = "v17.0";
    
    /// <summary>
    /// Varsayılan alıcı
    /// </summary>
    public string? DefaultRecipient { get; set; }
}

/// <summary>
/// LinkedIn konfigürasyonu
/// </summary>
public class LinkedInConfiguration : PlatformConfiguration
{
    /// <summary>
    /// Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Kullanıcı URN
    /// </summary>
    public string? UserUrn { get; set; }
    
    /// <summary>
    /// Şirket sayfası URN
    /// </summary>
    public string? CompanyPageUrn { get; set; }
    
    /// <summary>
    /// Otomatik paylaşım
    /// </summary>
    public bool AutoShare { get; set; } = true;
    
    /// <summary>
    /// Şirket sayfasından paylaş
    /// </summary>
    public bool ShareFromCompanyPage { get; set; } = false;
}

/// <summary>
/// Ana sosyal medya konfigürasyonu
/// </summary>
public class SocialMediaConfiguration
{
    /// <summary>
    /// Telegram konfigürasyonu
    /// </summary>
    public TelegramConfiguration Telegram { get; set; } = new();
    
    /// <summary>
    /// Twitter konfigürasyonu
    /// </summary>
    public TwitterConfiguration Twitter { get; set; } = new();
    
    /// <summary>
    /// Discord konfigürasyonu
    /// </summary>
    public DiscordConfiguration Discord { get; set; } = new();
    
    /// <summary>
    /// Slack konfigürasyonu
    /// </summary>
    public SlackConfiguration Slack { get; set; } = new();
    
    /// <summary>
    /// WhatsApp konfigürasyonu
    /// </summary>
    public WhatsAppConfiguration WhatsApp { get; set; } = new();
    
    /// <summary>
    /// LinkedIn konfigürasyonu
    /// </summary>
    public LinkedInConfiguration LinkedIn { get; set; } = new();
    
    /// <summary>
    /// Yeniden deneme politikası
    /// </summary>
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
}

/// <summary>
/// Yeniden deneme politikası konfigürasyonu
/// </summary>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// Maksimum deneme sayısı
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Temel gecikme
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Gecikme çarpanı
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Maksimum gecikme
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Jitter faktörü
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
} 