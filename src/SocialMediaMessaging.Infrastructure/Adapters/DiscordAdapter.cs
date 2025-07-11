using SocialMediaMessaging.Infrastructure.Configuration;
using CoreMessageType = SocialMediaMessaging.Core.Enums.MessageType;

namespace SocialMediaMessaging.Infrastructure.Adapters;

/// <summary>
/// Discord platform adaptörü
/// </summary>
public class DiscordAdapter : BasePlatformAdapter
{
    private readonly DiscordConfiguration _config;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public DiscordAdapter(
        ILogger<DiscordAdapter> logger,
        HttpClient httpClient,
        IOptions<SocialMediaConfiguration> options,
        IMemoryCache cache)
        : base(logger, httpClient, options.Value.Discord, cache)
    {
        _config = options.Value.Discord;
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "SocialMediaMessaging/1.0");
    }

    /// <summary>
    /// Platform adı
    /// </summary>
    public override string PlatformName => "Discord";

    /// <summary>
    /// Platform kısıtlamaları
    /// </summary>
    public override PlatformConstraints Constraints => new()
    {
        MaxCharacters = 2000,
        MaxAttachments = 10,
        SupportedAttachmentTypes = new List<string>
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/mov", "video/avi", "video/wmv", "video/webm",
            "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a",
            "application/pdf", "text/plain", "application/zip"
        },
        SupportsRichText = true,
        SupportsMarkdown = true,
        SupportsHTML = false,
        RateLimit = TimeSpan.FromSeconds(1),
        RateLimitCount = 5,
        MaxAttachmentSize = 8 * 1024 * 1024, // 8MB for normal users, 50MB for nitro
        SupportsScheduling = false,
        SupportsEditing = true,
        SupportsDeletion = true,
        SupportsEmoji = true,
        SupportsMentions = true,
        SupportsHashtags = false,
        SupportsThreads = true
    };

    /// <summary>
    /// Platform konfigürasyonu geçerli mi?
    /// </summary>
    public override async Task<bool> IsConfigurationValidAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.WebhookUrl))
            {
                Logger.LogError("Discord webhook URL boş");
                return false;
            }

            // Webhook URL formatını kontrol et
            if (!_config.WebhookUrl.Contains("discord.com/api/webhooks/"))
            {
                Logger.LogError("Geçersiz Discord webhook URL formatı");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Discord konfigürasyonu geçersiz");
            return false;
        }
    }

    /// <summary>
    /// Platform bağlantısını test eder
    /// </summary>
    public override async Task<bool> TestConnectionAsync()
    {
        try
        {
            var testPayload = new
            {
                content = "Test mesajı - Bağlantı kontrolü",
                username = _config.Username ?? "SocialMediaMessaging"
            };

            var jsonContent = JsonSerializer.Serialize(testPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(_config.WebhookUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Discord bağlantı testi başarılı");
                return true;
            }
            else
            {
                Logger.LogError("Discord bağlantı testi başarısız: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Discord bağlantı testi başarısız");
            return false;
        }
    }

    /// <summary>
    /// Desteklenen mesaj tiplerini döner
    /// </summary>
    public override List<CoreMessageType> GetSupportedMessageTypes()
    {
        return new List<CoreMessageType>
        {
            CoreMessageType.Text,
            CoreMessageType.RichText,
            CoreMessageType.Image,
            CoreMessageType.Video,
            CoreMessageType.Document
        };
    }

    /// <summary>
    /// Platform özel parametreleri döner
    /// </summary>
    public override Dictionary<string, object> GetPlatformParameters()
    {
        return new Dictionary<string, object>
        {
            { "maxMessageLength", 2000 },
            { "maxEmbedFields", 25 },
            { "maxEmbedLength", 6000 },
            { "supportsEmbeds", _config.UseEmbed },
            { "supportsTTS", _config.UseTTS },
            { "supportsThreads", true },
            { "supportsMarkdown", true },
            { "username", _config.Username ?? "" },
            { "avatarUrl", _config.AvatarUrl ?? "" },
            { "embedColor", _config.EmbedColor },
            { "maxAttachmentSize", 8 * 1024 * 1024 }, // 8MB
            { "maxAttachments", 10 }
        };
    }

    /// <summary>
    /// Platform özel mesaj gönderimi
    /// </summary>
    protected override async Task<PlatformResult> SendMessageInternalAsync(MessageRequest message, CancellationToken cancellationToken)
    {
        try
        {
            await RecordRateLimitAsync(message.UserId ?? "default");

            object payload;

            if (_config.UseEmbed && message.Type == CoreMessageType.RichText)
            {
                payload = CreateEmbedPayload(message);
            }
            else
            {
                payload = CreateSimplePayload(message);
            }

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(_config.WebhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Discord webhook response genellikle boş gelir, mesaj ID'si almak için farklı endpoint gerekir
                return PlatformResult.CreateSuccess(
                    PlatformName,
                    Guid.NewGuid().ToString(), // Geçici ID
                    null);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError("Discord mesaj gönderimi başarısız: {StatusCode}, {Error}", 
                    response.StatusCode, errorContent);
                
                return PlatformResult.CreateFailure(PlatformName, 
                    $"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Discord mesaj gönderimi başarısız");
            return PlatformResult.CreateFailure(PlatformName, ex.Message, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Embed payload oluşturur
    /// </summary>
    private object CreateEmbedPayload(MessageRequest message)
    {
        var embed = new
        {
            title = message.Title,
            description = TransformContent(message.Content, message.Type),
            color = ConvertColorToInt(_config.EmbedColor),
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            footer = new
            {
                text = "SocialMediaMessaging"
            }
        };

        var payload = new
        {
            username = _config.Username ?? "SocialMediaMessaging",
            avatar_url = _config.AvatarUrl,
            embeds = new[] { embed },
            tts = _config.UseTTS
        };

        return payload;
    }

    /// <summary>
    /// Basit payload oluşturur
    /// </summary>
    private object CreateSimplePayload(MessageRequest message)
    {
        var content = TransformContent(message.Content, message.Type);
        
        // Başlık varsa içeriğe ekle
        if (!string.IsNullOrWhiteSpace(message.Title))
        {
            content = $"**{message.Title}**\n{content}";
        }

        return new
        {
            content = content,
            username = _config.Username ?? "SocialMediaMessaging",
            avatar_url = _config.AvatarUrl,
            tts = _config.UseTTS
        };
    }

    /// <summary>
    /// Renk string'ini integer'a dönüştürür
    /// </summary>
    private int ConvertColorToInt(string colorHex)
    {
        try
        {
            if (colorHex.StartsWith("#"))
                colorHex = colorHex.Substring(1);
            
            return Convert.ToInt32(colorHex, 16);
        }
        catch
        {
            return 0x0099ff; // Varsayılan mavi
        }
    }

    /// <summary>
    /// Zengin metin içeriğini dönüştürür
    /// </summary>
    protected override string TransformRichTextContent(string content)
    {
        // Discord Markdown formatına dönüştür
        return content
            .Replace("<b>", "**").Replace("</b>", "**")
            .Replace("<strong>", "**").Replace("</strong>", "**")
            .Replace("<i>", "*").Replace("</i>", "*")
            .Replace("<em>", "*").Replace("</em>", "*")
            .Replace("<u>", "__").Replace("</u>", "__")
            .Replace("<s>", "~~").Replace("</s>", "~~")
            .Replace("<code>", "`").Replace("</code>", "`")
            .Replace("<pre>", "```").Replace("</pre>", "```")
            .Replace("<br>", "\n").Replace("<br/>", "\n")
            .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
    }

    /// <summary>
    /// Platform özel mesaj doğrulama
    /// </summary>
    protected override async Task<bool> ValidateMessageInternalAsync(MessageRequest message)
    {
        // Webhook URL kontrolü
        if (string.IsNullOrWhiteSpace(_config.WebhookUrl))
        {
            Logger.LogWarning("Discord webhook URL boş");
            return false;
        }

        // İçerik uzunluğu kontrolü
        var content = TransformContent(message.Content, message.Type);
        if (content.Length > 2000)
        {
            Logger.LogWarning("Discord mesaj çok uzun: {Length} karakter", content.Length);
            return false;
        }

        // Embed kontrolü
        if (_config.UseEmbed && message.Type == CoreMessageType.RichText)
        {
            // Embed description limiti
            if (content.Length > 4096)
            {
                Logger.LogWarning("Discord embed açıklaması çok uzun: {Length} karakter", content.Length);
                return false;
            }

            // Embed title limiti
            if (!string.IsNullOrEmpty(message.Title) && message.Title.Length > 256)
            {
                Logger.LogWarning("Discord embed başlığı çok uzun: {Length} karakter", message.Title.Length);
                return false;
            }
        }

        // Ek boyut kontrolü
        foreach (var attachment in message.Attachments)
        {
            if (attachment.Size > 8 * 1024 * 1024) // 8MB limit (normal users)
            {
                Logger.LogWarning("Discord ek çok büyük: {FileName}, Size: {Size}",
                    attachment.FileName, attachment.Size);
                return false;
            }
        }

        return true;
    }
} 