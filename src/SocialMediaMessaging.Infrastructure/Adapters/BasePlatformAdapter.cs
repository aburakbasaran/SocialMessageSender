using SocialMediaMessaging.Infrastructure.Configuration;
using Polly;

namespace SocialMediaMessaging.Infrastructure.Adapters;

/// <summary>
/// Platform adaptörü temel sınıfı
/// </summary>
public abstract class BasePlatformAdapter : IPlatformAdapter
{
    protected readonly ILogger Logger;
    protected readonly HttpClient HttpClient;
    protected readonly PlatformConfiguration Configuration;
    protected readonly IMemoryCache Cache;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    protected BasePlatformAdapter(
        ILogger logger,
        HttpClient httpClient,
        PlatformConfiguration configuration,
        IMemoryCache cache)
    {
        Logger = logger;
        HttpClient = httpClient;
        Configuration = configuration;
        Cache = cache;
    }

    /// <summary>
    /// Platform adı
    /// </summary>
    public abstract string PlatformName { get; }

    /// <summary>
    /// Platform kısıtlamaları
    /// </summary>
    public abstract PlatformConstraints Constraints { get; }

    /// <summary>
    /// Platform aktif mi?
    /// </summary>
    public virtual bool IsEnabled => Configuration.Enabled;

    /// <summary>
    /// Mesaj gönderir
    /// </summary>
    public async Task<PlatformResult> SendMessageAsync(MessageRequest message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogDebug("Mesaj gönderiliyor: {Platform}, RequestId: {RequestId}", PlatformName, message.RequestId);
            
            // Mesaj doğrulama
            if (!await ValidateMessageAsync(message))
            {
                return PlatformResult.CreateFailure(PlatformName, "Mesaj doğrulama başarısız");
            }

            // Rate limit kontrolü
            if (!await CheckRateLimitAsync(message.UserId ?? "default"))
            {
                return PlatformResult.CreateFailure(PlatformName, "Rate limit aşıldı");
            }

            // Platform özel gönderim
            var result = await SendMessageInternalAsync(message, cancellationToken);
            
            stopwatch.Stop();
            result = result with { ResponseTimeMs = stopwatch.ElapsedMilliseconds };
            
            Logger.LogInformation("Mesaj gönderim sonucu: {Platform}, Success: {Success}, Time: {Time}ms", 
                PlatformName, result.Success, result.ResponseTimeMs);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Mesaj gönderim hatası: {Platform}, RequestId: {RequestId}", 
                PlatformName, message.RequestId);
            
            return PlatformResult.CreateFailure(PlatformName, ex.Message, ex.GetType().Name, ex.StackTrace);
        }
    }

    /// <summary>
    /// Mesajı doğrular
    /// </summary>
    public virtual async Task<bool> ValidateMessageAsync(MessageRequest message)
    {
        // Temel doğrulama
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            Logger.LogWarning("Mesaj içeriği boş: {Platform}", PlatformName);
            return false;
        }

        if (message.Content.Length > Constraints.MaxCharacters)
        {
            Logger.LogWarning("Mesaj çok uzun: {Platform}, Length: {Length}, Max: {Max}", 
                PlatformName, message.Content.Length, Constraints.MaxCharacters);
            return false;
        }

        if (message.Attachments.Count > Constraints.MaxAttachments)
        {
            Logger.LogWarning("Çok fazla ek: {Platform}, Count: {Count}, Max: {Max}", 
                PlatformName, message.Attachments.Count, Constraints.MaxAttachments);
            return false;
        }

        // Ek doğrulama
        foreach (var attachment in message.Attachments)
        {
            if (!attachment.IsValid)
            {
                Logger.LogWarning("Geçersiz ek: {Platform}, FileName: {FileName}", 
                    PlatformName, attachment.FileName);
                return false;
            }

            if (attachment.Size > Constraints.MaxAttachmentSize)
            {
                Logger.LogWarning("Ek çok büyük: {Platform}, Size: {Size}, Max: {Max}", 
                    PlatformName, attachment.Size, Constraints.MaxAttachmentSize);
                return false;
            }

            if (Constraints.SupportedAttachmentTypes.Count > 0 && 
                !Constraints.SupportedAttachmentTypes.Contains(attachment.ContentType))
            {
                Logger.LogWarning("Desteklenmeyen ek tipi: {Platform}, Type: {Type}", 
                    PlatformName, attachment.ContentType);
                return false;
            }
        }

        return await ValidateMessageInternalAsync(message);
    }

    /// <summary>
    /// İçeriği platforma göre dönüştürür
    /// </summary>
    public virtual string TransformContent(string content, MessageType type)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        return type switch
        {
            MessageType.Text => TransformTextContent(content),
            MessageType.RichText => TransformRichTextContent(content),
            _ => content
        };
    }

    /// <summary>
    /// Platform konfigürasyonu geçerli mi?
    /// </summary>
    public abstract Task<bool> IsConfigurationValidAsync();

    /// <summary>
    /// Platform bağlantısını test eder
    /// </summary>
    public abstract Task<bool> TestConnectionAsync();

    /// <summary>
    /// Platform rate limit'ini kontrol eder
    /// </summary>
    public virtual Task<bool> CheckRateLimitAsync(string identifier)
    {
        var cacheKey = $"rate_limit:{PlatformName}:{identifier}";
        
        if (Cache.TryGetValue(cacheKey, out var cachedCount))
        {
            var requestCount = (int)cachedCount!;
            if (requestCount >= Constraints.RateLimitCount)
            {
                Logger.LogWarning("Rate limit aşıldı: {Platform}, Identifier: {Identifier}, Count: {Count}", 
                    PlatformName, identifier, requestCount);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Rate limit kaydeder
    /// </summary>
    protected virtual Task RecordRateLimitAsync(string identifier)
    {
        var cacheKey = $"rate_limit:{PlatformName}:{identifier}";
        
        if (Cache.TryGetValue(cacheKey, out var cachedCount))
        {
            var requestCount = (int)cachedCount! + 1;
            Cache.Set(cacheKey, requestCount, Constraints.RateLimit);
        }
        else
        {
            Cache.Set(cacheKey, 1, Constraints.RateLimit);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Desteklenen mesaj tiplerini döner
    /// </summary>
    public abstract List<MessageType> GetSupportedMessageTypes();

    /// <summary>
    /// Platform özel parametreleri döner
    /// </summary>
    public abstract Dictionary<string, object> GetPlatformParameters();

    /// <summary>
    /// Platform özel mesaj gönderimi
    /// </summary>
    protected abstract Task<PlatformResult> SendMessageInternalAsync(MessageRequest message, CancellationToken cancellationToken);

    /// <summary>
    /// Platform özel mesaj doğrulama
    /// </summary>
    protected virtual Task<bool> ValidateMessageInternalAsync(MessageRequest message)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Metin içeriğini dönüştürür
    /// </summary>
    protected virtual string TransformTextContent(string content)
    {
        return content;
    }

    /// <summary>
    /// Zengin metin içeriğini dönüştürür
    /// </summary>
    protected virtual string TransformRichTextContent(string content)
    {
        // HTML'i platform özel formata dönüştür
        if (Constraints.SupportsHTML)
            return content;
        
        if (Constraints.SupportsMarkdown)
            return ConvertHtmlToMarkdown(content);
        
        return StripHtmlTags(content);
    }

    /// <summary>
    /// HTML'i Markdown'a dönüştürür
    /// </summary>
    protected virtual string ConvertHtmlToMarkdown(string html)
    {
        // Basit HTML -> Markdown dönüşümü
        return html
            .Replace("<b>", "**").Replace("</b>", "**")
            .Replace("<strong>", "**").Replace("</strong>", "**")
            .Replace("<i>", "*").Replace("</i>", "*")
            .Replace("<em>", "*").Replace("</em>", "*")
            .Replace("<u>", "_").Replace("</u>", "_")
            .Replace("<code>", "`").Replace("</code>", "`")
            .Replace("<pre>", "```").Replace("</pre>", "```")
            .Replace("<br>", "\n").Replace("<br/>", "\n")
            .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
    }

    /// <summary>
    /// HTML etiketlerini kaldırır
    /// </summary>
    protected virtual string StripHtmlTags(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }

    /// <summary>
    /// Polly retry politikası
    /// </summary>
    protected virtual IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: Configuration.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Logger.LogWarning("Yeniden deneme: {Platform}, Attempt: {Attempt}, Delay: {Delay}ms", 
                        PlatformName, retryCount, timespan.TotalMilliseconds);
                });
    }
} 