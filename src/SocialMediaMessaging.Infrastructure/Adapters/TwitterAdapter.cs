using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using SocialMediaMessaging.Infrastructure.Configuration;
using CoreMessageType = SocialMediaMessaging.Core.Enums.MessageType;

namespace SocialMediaMessaging.Infrastructure.Adapters;

/// <summary>
/// Twitter platform adaptörü
/// </summary>
public class TwitterAdapter : BasePlatformAdapter
{
    private readonly ITwitterClient _twitterClient;
    private readonly TwitterConfiguration _config;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public TwitterAdapter(
        ILogger<TwitterAdapter> logger,
        HttpClient httpClient,
        IOptions<SocialMediaConfiguration> options,
        IMemoryCache cache)
        : base(logger, httpClient, options.Value.Twitter, cache)
    {
        _config = options.Value.Twitter;
        _twitterClient = new TwitterClient(_config.ApiKey, _config.ApiSecret, _config.AccessToken, _config.AccessTokenSecret);
    }

    /// <summary>
    /// Platform adı
    /// </summary>
    public override string PlatformName => "Twitter";

    /// <summary>
    /// Platform kısıtlamaları
    /// </summary>
    public override PlatformConstraints Constraints => new()
    {
        MaxCharacters = 280,
        MaxAttachments = 4,
        SupportedAttachmentTypes = new List<string>
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/mov", "video/avi", "video/wmv"
        },
        SupportsRichText = false,
        SupportsMarkdown = false,
        SupportsHTML = false,
        RateLimit = TimeSpan.FromMinutes(15),
        RateLimitCount = 300,
        MaxAttachmentSize = 512 * 1024 * 1024, // 512MB for videos, 5MB for images
        SupportsScheduling = false,
        SupportsEditing = false,
        SupportsDeletion = true,
        SupportsEmoji = true,
        SupportsMentions = true,
        SupportsHashtags = true,
        SupportsThreads = true
    };

    /// <summary>
    /// Platform konfigürasyonu geçerli mi?
    /// </summary>
    public override async Task<bool> IsConfigurationValidAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey) ||
                string.IsNullOrWhiteSpace(_config.ApiSecret) ||
                string.IsNullOrWhiteSpace(_config.AccessToken) ||
                string.IsNullOrWhiteSpace(_config.AccessTokenSecret))
            {
                Logger.LogError("Twitter API bilgileri eksik");
                return false;
            }

            var user = await _twitterClient.Users.GetAuthenticatedUserAsync();
            if (user == null)
            {
                Logger.LogError("Twitter kullanıcı bilgileri alınamadı");
                return false;
            }

            Logger.LogInformation("Twitter kullanıcı bilgileri: {ScreenName} ({Id})", user.ScreenName, user.Id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Twitter konfigürasyonu geçersiz");
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
            var user = await _twitterClient.Users.GetAuthenticatedUserAsync();
            return user != null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Twitter bağlantı testi başarısız");
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
            CoreMessageType.Image,
            CoreMessageType.Video
        };
    }

    /// <summary>
    /// Platform özel parametreleri döner
    /// </summary>
    public override Dictionary<string, object> GetPlatformParameters()
    {
        return new Dictionary<string, object>
        {
            { "maxTweetLength", 280 },
            { "maxMediaItems", 4 },
            { "supportsThreads", true },
            { "supportsHashtags", true },
            { "supportsMentions", true },
            { "autoAddHashtags", _config.AutoAddHashtags },
            { "defaultHashtags", _config.DefaultHashtags },
            { "maxImageSize", 5 * 1024 * 1024 }, // 5MB
            { "maxVideoSize", 512 * 1024 * 1024 }, // 512MB
            { "supportedImageFormats", new[] { "jpeg", "png", "gif", "webp" } },
            { "supportedVideoFormats", new[] { "mp4", "mov", "avi", "wmv" } }
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

            var content = PrepareContent(message);
            
            ITweet tweet;

            if (message.Attachments.Count > 0)
            {
                tweet = await SendTweetWithMediaAsync(content, message.Attachments, cancellationToken);
            }
            else
            {
                tweet = await _twitterClient.Tweets.PublishTweetAsync(content);
            }

            if (tweet == null)
            {
                return PlatformResult.CreateFailure(PlatformName, "Tweet gönderilemedi");
            }

            var tweetUrl = $"https://twitter.com/{tweet.CreatedBy.ScreenName}/status/{tweet.Id}";

            return PlatformResult.CreateSuccess(
                PlatformName,
                tweet.Id.ToString(),
                tweetUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Twitter mesaj gönderimi başarısız");
            return PlatformResult.CreateFailure(PlatformName, ex.Message, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Medya ile tweet gönderir
    /// </summary>
    private async Task<ITweet> SendTweetWithMediaAsync(string content, List<Attachment> attachments, CancellationToken cancellationToken)
    {
        var mediaIds = new List<long>();

        foreach (var attachment in attachments.Take(4)) // Twitter max 4 medya
        {
            var media = await UploadMediaAsync(attachment, cancellationToken);
            if (media != null)
            {
                mediaIds.Add(media.Id!.Value);
            }
        }

        var tweetParameters = new PublishTweetParameters(content)
        {
            MediaIds = mediaIds
        };

        return await _twitterClient.Tweets.PublishTweetAsync(tweetParameters);
    }

    /// <summary>
    /// Medya yükler
    /// </summary>
    private async Task<IMedia?> UploadMediaAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        try
        {
            var mediaData = attachment.Data;
            if (mediaData == null || mediaData.Length == 0)
            {
                Logger.LogWarning("Medya verisi boş: {FileName}", attachment.FileName);
                return null;
            }

            // Basit binary upload
            var media = await _twitterClient.Upload.UploadBinaryAsync(mediaData);
            
            if (media == null)
            {
                Logger.LogWarning("Medya yüklenemedi: {FileName}", attachment.FileName);
                return null;
            }

            // Video için processing bekle
            if (attachment.ContentType.StartsWith("video/"))
            {
                await WaitForMediaProcessingAsync(media);
            }

            return media;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Medya yükleme hatası: {FileName}", attachment.FileName);
            return null;
        }
    }

    /// <summary>
    /// Medya kategorisini belirler
    /// </summary>
    private string GetMediaCategory(string contentType)
    {
        return contentType switch
        {
            var ct when ct.StartsWith("image/") => "tweet_image",
            var ct when ct.StartsWith("video/") => "tweet_video",
            _ => "tweet_image"
        };
    }

    /// <summary>
    /// Medya işlenmesini bekler
    /// </summary>
    private async Task WaitForMediaProcessingAsync(IMedia media)
    {
        var maxWaitTime = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;
        var checkInterval = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            try
            {
                // TweetinviAPI'de GetMediaStatusAsync metodu mevcut olmayabilir
                // Bu durumda sadece bekleme yapıyoruz
                await Task.Delay(checkInterval);
                
                // Basit bir control - 30 saniye bekle
                if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(30))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Medya durumu kontrol edilemedi: {MediaId}", media.Id);
                break;
            }
        }
    }

    /// <summary>
    /// Tweet içeriğini hazırlar
    /// </summary>
    private string PrepareContent(MessageRequest message)
    {
        var content = message.Content;

        // Hashtag ekleme
        if (_config.AutoAddHashtags && _config.DefaultHashtags.Count > 0)
        {
            var hashtags = string.Join(" ", _config.DefaultHashtags.Select(h => h.StartsWith("#") ? h : $"#{h}"));
            
            // Karakter limitini kontrol et
            if (content.Length + hashtags.Length + 1 <= 280)
            {
                content = $"{content} {hashtags}";
            }
        }

        // Karakter limitini kontrol et
        if (content.Length > 280)
        {
            content = content.Substring(0, 277) + "...";
        }

        return content;
    }

    /// <summary>
    /// Metin içeriğini dönüştürür
    /// </summary>
    protected override string TransformTextContent(string content)
    {
        // Twitter özel karakterleri dönüştür
        return content
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'");
    }

    /// <summary>
    /// Platform özel mesaj doğrulama
    /// </summary>
    protected override async Task<bool> ValidateMessageInternalAsync(MessageRequest message)
    {
        // İçerik uzunluğu kontrolü
        var content = PrepareContent(message);
        if (content.Length > 280)
        {
            Logger.LogWarning("Tweet çok uzun: {Length} karakter", content.Length);
            return false;
        }

        // Medya kontrolü
        if (message.Attachments.Count > 4)
        {
            Logger.LogWarning("Twitter maksimum 4 medya destekler");
            return false;
        }

        // Medya boyut kontrolü
        foreach (var attachment in message.Attachments)
        {
            var maxSize = attachment.ContentType.StartsWith("image/") ? 5 * 1024 * 1024 : 512 * 1024 * 1024;
            
            if (attachment.Size > maxSize)
            {
                Logger.LogWarning("Medya çok büyük: {FileName}, Size: {Size}, Max: {Max}",
                    attachment.FileName, attachment.Size, maxSize);
                return false;
            }
        }

        return true;
    }
} 