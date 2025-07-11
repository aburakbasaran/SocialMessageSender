using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using SocialMediaMessaging.Infrastructure.Configuration;
using CoreMessageType = SocialMediaMessaging.Core.Enums.MessageType;

namespace SocialMediaMessaging.Infrastructure.Adapters;

/// <summary>
/// Telegram platform adaptörü
/// </summary>
public class TelegramAdapter : BasePlatformAdapter
{
    private readonly TelegramBotClient _botClient;
    private readonly TelegramConfiguration _config;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public TelegramAdapter(
        ILogger<TelegramAdapter> logger,
        HttpClient httpClient,
        IOptions<SocialMediaConfiguration> options,
        IMemoryCache cache)
        : base(logger, httpClient, options.Value.Telegram, cache)
    {
        _config = options.Value.Telegram;
        _botClient = new TelegramBotClient(_config.BotToken);
    }

    /// <summary>
    /// Platform adı
    /// </summary>
    public override string PlatformName => "Telegram";

    /// <summary>
    /// Platform kısıtlamaları
    /// </summary>
    public override PlatformConstraints Constraints => new()
    {
        MaxCharacters = 4096,
        MaxAttachments = 10,
        SupportedAttachmentTypes = new List<string>
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/avi", "video/mov", "video/wmv",
            "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a",
            "application/pdf", "application/zip", "application/rar",
            "text/plain", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        },
        SupportsRichText = true,
        SupportsMarkdown = true,
        SupportsHTML = true,
        RateLimit = TimeSpan.FromSeconds(1),
        RateLimitCount = 30,
        MaxAttachmentSize = 50 * 1024 * 1024, // 50MB
        SupportsScheduling = false,
        SupportsEditing = true,
        SupportsDeletion = true,
        SupportsEmoji = true,
        SupportsMentions = true,
        SupportsHashtags = false,
        SupportsThreads = false
    };

    /// <summary>
    /// Platform konfigürasyonu geçerli mi?
    /// </summary>
    public override async Task<bool> IsConfigurationValidAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.BotToken))
            {
                Logger.LogError("Telegram bot token boş");
                return false;
            }

            var me = await _botClient.GetMeAsync();
            Logger.LogInformation("Telegram bot bilgileri: {Username} ({Id})", me.Username, me.Id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Telegram konfigürasyonu geçersiz");
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
            await _botClient.GetMeAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Telegram bağlantı testi başarısız");
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
            { "parseMode", _config.ParseMode },
            { "disableNotification", _config.DisableNotification },
            { "disableWebPagePreview", _config.DisableWebPagePreview },
            { "defaultChatId", _config.DefaultChatId ?? "" },
            { "supportsInlineKeyboard", true },
            { "supportsReplyMarkup", true },
            { "maxMessageLength", 4096 },
            { "maxCaptionLength", 1024 }
        };
    }

    /// <summary>
    /// Platform özel mesaj gönderimi
    /// </summary>
    protected override async Task<PlatformResult> SendMessageInternalAsync(MessageRequest message, CancellationToken cancellationToken)
    {
        try
        {
            var chatId = GetChatId(message);
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return PlatformResult.CreateFailure(PlatformName, "Chat ID bulunamadı");
            }

            await RecordRateLimitAsync(message.UserId ?? "default");

            var content = TransformContent(message.Content, message.Type);
            var parseMode = GetParseMode(message.Type);

            Message sentMessage;

            if (message.Attachments.Count > 0)
            {
                sentMessage = await SendMessageWithAttachmentsAsync(chatId, content, message.Attachments, parseMode, cancellationToken);
            }
            else
            {
                sentMessage = await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: content,
                    parseMode: parseMode,
                    disableWebPagePreview: _config.DisableWebPagePreview,
                    disableNotification: _config.DisableNotification,
                    cancellationToken: cancellationToken);
            }

            var messageUrl = $"https://t.me/{chatId.Replace("@", "")}/{sentMessage.MessageId}";

            return PlatformResult.CreateSuccess(
                PlatformName,
                sentMessage.MessageId.ToString(),
                messageUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Telegram mesaj gönderimi başarısız");
            return PlatformResult.CreateFailure(PlatformName, ex.Message, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Ekli mesaj gönderir
    /// </summary>
    private async Task<Message> SendMessageWithAttachmentsAsync(
        string chatId,
        string content,
        List<Attachment> attachments,
        ParseMode parseMode,
        CancellationToken cancellationToken)
    {
        var attachment = attachments.First();

        return attachment.ContentType switch
        {
            var ct when ct.StartsWith("image/") => await SendPhotoAsync(chatId, content, attachment, parseMode, cancellationToken),
            var ct when ct.StartsWith("video/") => await SendVideoAsync(chatId, content, attachment, parseMode, cancellationToken),
            var ct when ct.StartsWith("audio/") => await SendAudioAsync(chatId, content, attachment, parseMode, cancellationToken),
            _ => await SendDocumentAsync(chatId, content, attachment, parseMode, cancellationToken)
        };
    }

    /// <summary>
    /// Fotoğraf gönderir
    /// </summary>
    private async Task<Message> SendPhotoAsync(
        string chatId,
        string content,
        Attachment attachment,
        ParseMode parseMode,
        CancellationToken cancellationToken)
    {
        var inputFile = InputFile.FromStream(new MemoryStream(attachment.Data!), attachment.FileName);

        return await _botClient.SendPhotoAsync(
            chatId: chatId,
            photo: inputFile,
            caption: content,
            parseMode: parseMode,
            disableNotification: _config.DisableNotification,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Video gönderir
    /// </summary>
    private async Task<Message> SendVideoAsync(
        string chatId,
        string content,
        Attachment attachment,
        ParseMode parseMode,
        CancellationToken cancellationToken)
    {
        var inputFile = InputFile.FromStream(new MemoryStream(attachment.Data!), attachment.FileName);

        return await _botClient.SendVideoAsync(
            chatId: chatId,
            video: inputFile,
            caption: content,
            parseMode: parseMode,
            disableNotification: _config.DisableNotification,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Ses gönderir
    /// </summary>
    private async Task<Message> SendAudioAsync(
        string chatId,
        string content,
        Attachment attachment,
        ParseMode parseMode,
        CancellationToken cancellationToken)
    {
        var inputFile = InputFile.FromStream(new MemoryStream(attachment.Data!), attachment.FileName);

        return await _botClient.SendAudioAsync(
            chatId: chatId,
            audio: inputFile,
            caption: content,
            parseMode: parseMode,
            disableNotification: _config.DisableNotification,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Döküman gönderir
    /// </summary>
    private async Task<Message> SendDocumentAsync(
        string chatId,
        string content,
        Attachment attachment,
        ParseMode parseMode,
        CancellationToken cancellationToken)
    {
        var inputFile = InputFile.FromStream(new MemoryStream(attachment.Data!), attachment.FileName);

        return await _botClient.SendDocumentAsync(
            chatId: chatId,
            document: inputFile,
            caption: content,
            parseMode: parseMode,
            disableNotification: _config.DisableNotification,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Chat ID'yi belirler
    /// </summary>
    private string GetChatId(MessageRequest message)
    {
        // Platform özel verilerden chat ID'yi al
        if (message.PlatformSpecificData.TryGetValue("chatId", out var chatIdObj))
        {
            return chatIdObj.ToString() ?? "";
        }

        // Varsayılan chat ID'yi kullan
        return _config.DefaultChatId ?? "";
    }

    /// <summary>
    /// Parse mode'u belirler
    /// </summary>
    private ParseMode GetParseMode(CoreMessageType messageType)
    {
        return messageType switch
        {
            CoreMessageType.RichText when _config.ParseMode.ToLower() == "html" => ParseMode.Html,
            CoreMessageType.RichText when _config.ParseMode.ToLower() == "markdown" => ParseMode.Markdown,
            CoreMessageType.RichText when _config.ParseMode.ToLower() == "markdownv2" => ParseMode.MarkdownV2,
            _ => ParseMode.Html
        };
    }

    /// <summary>
    /// Zengin metin içeriğini dönüştürür
    /// </summary>
    protected override string TransformRichTextContent(string content)
    {
        return _config.ParseMode.ToLower() switch
        {
            "html" => content,
            "markdown" => ConvertHtmlToMarkdown(content),
            "markdownv2" => ConvertHtmlToMarkdownV2(content),
            _ => StripHtmlTags(content)
        };
    }

    /// <summary>
    /// HTML'i MarkdownV2'ye dönüştürür
    /// </summary>
    private string ConvertHtmlToMarkdownV2(string html)
    {
        // Telegram MarkdownV2 formatına dönüştürme
        return html
            .Replace("<b>", "*").Replace("</b>", "*")
            .Replace("<strong>", "*").Replace("</strong>", "*")
            .Replace("<i>", "_").Replace("</i>", "_")
            .Replace("<em>", "_").Replace("</em>", "_")
            .Replace("<u>", "__").Replace("</u>", "__")
            .Replace("<s>", "~").Replace("</s>", "~")
            .Replace("<code>", "`").Replace("</code>", "`")
            .Replace("<pre>", "```").Replace("</pre>", "```")
            .Replace("<br>", "\n").Replace("<br/>", "\n")
            .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
            // Özel karakterleri escape et
            .Replace(".", "\\.")
            .Replace("!", "\\!")
            .Replace("-", "\\-")
            .Replace("(", "\\(").Replace(")", "\\)")
            .Replace("[", "\\[").Replace("]", "\\]")
            .Replace("{", "\\{").Replace("}", "\\}")
            .Replace("+", "\\+")
            .Replace("=", "\\=")
            .Replace("|", "\\|");
    }

    /// <summary>
    /// Platform özel mesaj doğrulama
    /// </summary>
    protected override async Task<bool> ValidateMessageInternalAsync(MessageRequest message)
    {
        // Chat ID kontrolü
        var chatId = GetChatId(message);
        if (string.IsNullOrWhiteSpace(chatId))
        {
            Logger.LogWarning("Telegram chat ID bulunamadı");
            return false;
        }

        // Ek boyut kontrolü
        foreach (var attachment in message.Attachments)
        {
            if (attachment.Data == null || attachment.Data.Length == 0)
            {
                Logger.LogWarning("Telegram ek verisi boş: {FileName}", attachment.FileName);
                return false;
            }

            // Telegram dosya boyut limitleri
            var maxSize = attachment.ContentType switch
            {
                var ct when ct.StartsWith("image/") => 10 * 1024 * 1024, // 10MB
                var ct when ct.StartsWith("video/") => 50 * 1024 * 1024, // 50MB
                var ct when ct.StartsWith("audio/") => 50 * 1024 * 1024, // 50MB
                _ => 50 * 1024 * 1024 // 50MB
            };

            if (attachment.Size > maxSize)
            {
                Logger.LogWarning("Telegram ek çok büyük: {FileName}, Size: {Size}, Max: {Max}",
                    attachment.FileName, attachment.Size, maxSize);
                return false;
            }
        }

        return true;
    }
} 