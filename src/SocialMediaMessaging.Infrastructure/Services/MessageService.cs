namespace SocialMediaMessaging.Infrastructure.Services;

/// <summary>
/// Mesaj servisi implementasyonu
/// </summary>
public class MessageService : IMessageService
{
    private readonly IPlatformAdapterFactory _platformAdapterFactory;
    private readonly IMessageValidator _messageValidator;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IRetryService _retryService;
    private readonly ILogger<MessageService> _logger;
    private readonly IMemoryCache _cache;

    // In-memory storage for demo purposes (production'da database kullanılmalı)
    private readonly Dictionary<string, MessageResponse> _messageHistory = new();

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public MessageService(
        IPlatformAdapterFactory platformAdapterFactory,
        IMessageValidator messageValidator,
        IRateLimitingService rateLimitingService,
        IRetryService retryService,
        ILogger<MessageService> logger,
        IMemoryCache cache)
    {
        _platformAdapterFactory = platformAdapterFactory;
        _messageValidator = messageValidator;
        _rateLimitingService = rateLimitingService;
        _retryService = retryService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Tek mesaj gönderir
    /// </summary>
    public async Task<MessageResponse> SendMessageAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageId = request.RequestId;

        try
        {
            _logger.LogInformation("Mesaj gönderimi başlatılıyor: {MessageId}, Platforms: {Platforms}", 
                messageId, string.Join(", ", request.Platforms));

            // Mesaj doğrulama
            var validationResult = await _messageValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var validationFailureResponse = MessageResponse.CreateFailure(messageId, validationResult.Errors);
                _messageHistory[messageId] = validationFailureResponse;
                
                _logger.LogWarning("Mesaj doğrulama başarısız: {MessageId}, Errors: {Errors}", 
                    messageId, string.Join(", ", validationResult.Errors));
                
                return validationFailureResponse;
            }

            // Zamanlanmış mesaj kontrolü
            if (request.ScheduledAt.HasValue && request.ScheduledAt.Value > DateTime.UtcNow)
            {
                await ScheduleMessageAsync(request);
                var scheduledResponse = new MessageResponse
                {
                    MessageId = messageId,
                    Status = MessageStatus.Pending,
                    SentAt = DateTime.UtcNow
                };
                _messageHistory[messageId] = scheduledResponse;
                
                _logger.LogInformation("Mesaj zamanlandı: {MessageId}, ScheduledAt: {ScheduledAt}", 
                    messageId, request.ScheduledAt.Value);
                
                return scheduledResponse;
            }

            // Platform adaptörlerini al
            var adapters = request.Platforms
                .Select(platform => new { Platform = platform, Adapter = _platformAdapterFactory.GetAdapter(platform) })
                .Where(x => x.Adapter != null)
                .ToList();

            if (adapters.Count == 0)
            {
                var noPlatformResponse = MessageResponse.CreateFailure(messageId, new List<string> { "Hiç geçerli platform bulunamadı" });
                _messageHistory[messageId] = noPlatformResponse;
                
                _logger.LogError("Geçerli platform bulunamadı: {MessageId}, RequestedPlatforms: {Platforms}", 
                    messageId, string.Join(", ", request.Platforms));
                
                return noPlatformResponse;
            }

            // Paralel platform gönderimi
            var platformTasks = adapters.Select(async item =>
            {
                var platformName = item.Platform;
                var adapter = item.Adapter!;

                try
                {
                    _logger.LogDebug("Platform gönderimi başlatılıyor: {Platform}, MessageId: {MessageId}", 
                        platformName, messageId);

                    PlatformResult result;
                    
                    if (request.EnableRetry)
                    {
                        // Retry logic ile gönder
                        result = await _retryService.ExecuteWithRetryAsync(
                            async () => await adapter.SendMessageAsync(request, cancellationToken),
                            request.MaxRetryAttempts,
                            TimeSpan.FromSeconds(2),
                            cancellationToken);
                    }
                    else
                    {
                        // Direkt gönder
                        result = await adapter.SendMessageAsync(request, cancellationToken);
                    }

                    _logger.LogInformation("Platform gönderim sonucu: {Platform}, Success: {Success}, MessageId: {MessageId}", 
                        platformName, result.Success, messageId);

                    return new { Platform = platformName, Result = result };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Platform gönderim hatası: {Platform}, MessageId: {MessageId}", 
                        platformName, messageId);

                    return new
                    {
                        Platform = platformName,
                        Result = PlatformResult.CreateFailure(platformName, ex.Message, ex.GetType().Name)
                    };
                }
            });

            var platformResults = await Task.WhenAll(platformTasks);

            stopwatch.Stop();

            // Sonuçları topla
            var results = platformResults.ToDictionary(x => x.Platform, x => x.Result);
            var successCount = results.Count(r => r.Value.Success);
            var failureCount = results.Count - successCount;

            // Genel durum belirle
            var status = successCount == results.Count ? MessageStatus.Sent :
                        successCount > 0 ? MessageStatus.PartialSuccess :
                        MessageStatus.Failed;

            var response = new MessageResponse
            {
                MessageId = messageId,
                Status = status,
                PlatformResults = results,
                SentAt = DateTime.UtcNow,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Başarısız olanlar için hata mesajları topla
            if (failureCount > 0)
            {
                var errors = results.Where(r => !r.Value.Success)
                    .Select(r => $"{r.Key}: {r.Value.Error}")
                    .ToList();
                
                response = response with { Errors = errors };
            }

            _messageHistory[messageId] = response;

            _logger.LogInformation("Mesaj gönderimi tamamlandı: {MessageId}, Status: {Status}, Success: {Success}/{Total}, Time: {Time}ms", 
                messageId, status, successCount, results.Count, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Mesaj gönderim hatası: {MessageId}", messageId);

            var errorResponse = MessageResponse.CreateFailure(messageId, new List<string> { ex.Message });
            errorResponse = errorResponse with { ProcessingTimeMs = stopwatch.ElapsedMilliseconds };
            
            _messageHistory[messageId] = errorResponse;
            return errorResponse;
        }
    }

    /// <summary>
    /// Toplu mesaj gönderir
    /// </summary>
    public async Task<List<MessageResponse>> SendBulkMessageAsync(List<MessageRequest> requests, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toplu mesaj gönderimi başlatılıyor: {Count} mesaj", requests.Count);

        var tasks = requests.Select(request => SendMessageAsync(request, cancellationToken));
        var responses = await Task.WhenAll(tasks);

        var successCount = responses.Count(r => r.Status == MessageStatus.Sent || r.Status == MessageStatus.PartialSuccess);
        
        _logger.LogInformation("Toplu mesaj gönderimi tamamlandı: {Success}/{Total} başarılı", 
            successCount, requests.Count);

        return responses.ToList();
    }

    /// <summary>
    /// Mesaj durumunu sorgular
    /// </summary>
    public async Task<MessageResponse?> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            _logger.LogWarning("Mesaj ID boş");
            return null;
        }

        if (_messageHistory.TryGetValue(messageId, out var response))
        {
            _logger.LogDebug("Mesaj durumu bulundu: {MessageId}, Status: {Status}", messageId, response.Status);
            return response;
        }

        _logger.LogWarning("Mesaj bulunamadı: {MessageId}", messageId);
        return null;
    }

    /// <summary>
    /// Zamanlanmış mesajları işler
    /// </summary>
    public async Task<int> ProcessScheduledMessagesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Zamanlanmış mesajlar işleniyor");

        var scheduledKey = "scheduled_messages";
        var scheduledMessages = _cache.Get<List<MessageRequest>>(scheduledKey) ?? new List<MessageRequest>();

        var now = DateTime.UtcNow;
        var messagesToProcess = scheduledMessages
            .Where(m => m.ScheduledAt.HasValue && m.ScheduledAt.Value <= now)
            .ToList();

        if (messagesToProcess.Count == 0)
        {
            _logger.LogDebug("İşlenecek zamanlanmış mesaj yok");
            return 0;
        }

        _logger.LogInformation("Zamanlanmış mesajlar işleniyor: {Count} mesaj", messagesToProcess.Count);

        var processedCount = 0;
        foreach (var message in messagesToProcess)
        {
            try
            {
                // ScheduledAt'ı null yap ki tekrar zamanlanmasın
                var messageToSend = message with { ScheduledAt = null };
                
                await SendMessageAsync(messageToSend, cancellationToken);
                
                // İşlenen mesajı scheduled listesinden kaldır
                scheduledMessages.Remove(message);
                processedCount++;
                
                _logger.LogDebug("Zamanlanmış mesaj işlendi: {MessageId}", message.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Zamanlanmış mesaj işleme hatası: {MessageId}", message.RequestId);
            }
        }

        // Cache'i güncelle
        _cache.Set(scheduledKey, scheduledMessages, TimeSpan.FromDays(30));

        _logger.LogInformation("Zamanlanmış mesaj işleme tamamlandı: {Processed}/{Total} işlendi", 
            processedCount, messagesToProcess.Count);

        return processedCount;
    }

    /// <summary>
    /// Başarısız mesajları yeniden dener
    /// </summary>
    public async Task<MessageResponse> RetryFailedMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mesaj yeniden deneniyor: {MessageId}", messageId);

        var existingResponse = await GetMessageStatusAsync(messageId, cancellationToken);
        if (existingResponse == null)
        {
            _logger.LogWarning("Yeniden denenecek mesaj bulunamadı: {MessageId}", messageId);
            return MessageResponse.CreateFailure(messageId, new List<string> { "Mesaj bulunamadı" });
        }

        if (existingResponse.Status == MessageStatus.Sent)
        {
            _logger.LogInformation("Mesaj zaten başarıyla gönderilmiş: {MessageId}", messageId);
            return existingResponse;
        }

        // Başarısız platformları belirle
        var failedPlatforms = existingResponse.PlatformResults
            .Where(pr => !pr.Value.Success)
            .Select(pr => pr.Key)
            .ToList();

        if (failedPlatforms.Count == 0)
        {
            _logger.LogInformation("Yeniden denenecek başarısız platform yok: {MessageId}", messageId);
            return existingResponse;
        }

        // Orijinal mesaj request'ini cache'den al (demo için basit implementasyon)
        var retryKey = $"retry_request:{messageId}";
        var originalRequest = _cache.Get<MessageRequest>(retryKey);
        
        if (originalRequest == null)
        {
            _logger.LogError("Orijinal mesaj request'i bulunamadı: {MessageId}", messageId);
            return MessageResponse.CreateFailure(messageId, new List<string> { "Orijinal mesaj verisi bulunamadı" });
        }

        // Sadece başarısız platformlarda yeniden dene
        var retryRequest = originalRequest with 
        { 
            Platforms = failedPlatforms,
            RequestId = $"{messageId}_retry_{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        var retryResponse = await SendMessageAsync(retryRequest, cancellationToken);

        // Orijinal response'u güncelle
        var updatedPlatformResults = new Dictionary<string, PlatformResult>(existingResponse.PlatformResults);
        
        foreach (var platformResult in retryResponse.PlatformResults)
        {
            updatedPlatformResults[platformResult.Key] = platformResult.Value;
        }

        var successCount = updatedPlatformResults.Count(pr => pr.Value.Success);
        var newStatus = successCount == updatedPlatformResults.Count ? MessageStatus.Sent :
                       successCount > 0 ? MessageStatus.PartialSuccess :
                       MessageStatus.Failed;

        var updatedResponse = existingResponse with
        {
            Status = newStatus,
            PlatformResults = updatedPlatformResults,
            SentAt = DateTime.UtcNow
        };

        _messageHistory[messageId] = updatedResponse;

        _logger.LogInformation("Mesaj yeniden deneme tamamlandı: {MessageId}, NewStatus: {Status}", 
            messageId, newStatus);

        return updatedResponse;
    }

    /// <summary>
    /// Mesaj geçmişini getirir
    /// </summary>
    public async Task<List<MessageResponse>> GetMessageHistoryAsync(string? userId = null, int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mesaj geçmişi getiriliyor: UserId: {UserId}, Limit: {Limit}, Offset: {Offset}", 
            userId, limit, offset);

        var history = _messageHistory.Values.AsQueryable();

        // Kullanıcı filtresi (demo için basit implementasyon)
        if (!string.IsNullOrWhiteSpace(userId))
        {
            // Production'da database query ile yapılmalı
            history = history.Where(h => h.MessageId.Contains(userId));
        }

        var result = history
            .OrderByDescending(h => h.SentAt)
            .Skip(offset)
            .Take(limit)
            .ToList();

        _logger.LogDebug("Mesaj geçmişi getirildi: {Count} mesaj", result.Count);

        return result;
    }

    /// <summary>
    /// Mesaj istatistiklerini getirir
    /// </summary>
    public async Task<Dictionary<string, object>> GetMessageStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mesaj istatistikleri hesaplanıyor: From: {From}, To: {To}", from, to);

        var messages = _messageHistory.Values.AsQueryable();

        // Tarih filtresi
        if (from.HasValue)
            messages = messages.Where(m => m.SentAt >= from.Value);
        
        if (to.HasValue)
            messages = messages.Where(m => m.SentAt <= to.Value);

        var messageList = messages.ToList();

        var statistics = new Dictionary<string, object>
        {
            { "totalMessages", messageList.Count },
            { "successfulMessages", messageList.Count(m => m.Status == MessageStatus.Sent) },
            { "partiallySuccessfulMessages", messageList.Count(m => m.Status == MessageStatus.PartialSuccess) },
            { "failedMessages", messageList.Count(m => m.Status == MessageStatus.Failed) },
            { "pendingMessages", messageList.Count(m => m.Status == MessageStatus.Pending) },
            { "averageProcessingTime", messageList.Count > 0 ? messageList.Average(m => m.ProcessingTimeMs) : 0 },
            { "averageSuccessRate", messageList.Count > 0 ? messageList.Average(m => m.SuccessRate) : 0 },
            { "platformStatistics", GetPlatformStatistics(messageList) },
            { "timeRange", new { from, to } },
            { "generatedAt", DateTime.UtcNow }
        };

        _logger.LogDebug("Mesaj istatistikleri hesaplandı: {TotalMessages} mesaj", messageList.Count);

        return statistics;
    }

    /// <summary>
    /// Platform istatistiklerini hesaplar
    /// </summary>
    private Dictionary<string, object> GetPlatformStatistics(List<MessageResponse> messages)
    {
        var platformStats = new Dictionary<string, object>();

        var allPlatformResults = messages
            .SelectMany(m => m.PlatformResults)
            .GroupBy(pr => pr.Key)
            .ToList();

        foreach (var platformGroup in allPlatformResults)
        {
            var platformName = platformGroup.Key;
            var results = platformGroup.Select(g => g.Value).ToList();

            platformStats[platformName] = new
            {
                totalAttempts = results.Count,
                successfulAttempts = results.Count(r => r.Success),
                failedAttempts = results.Count(r => !r.Success),
                successRate = results.Count > 0 ? (double)results.Count(r => r.Success) / results.Count : 0,
                averageResponseTime = results.Count > 0 ? results.Average(r => r.ResponseTimeMs) : 0
            };
        }

        return platformStats;
    }

    /// <summary>
    /// Mesajı zamanlar
    /// </summary>
    private async Task ScheduleMessageAsync(MessageRequest request)
    {
        var scheduledKey = "scheduled_messages";
        var scheduledMessages = _cache.Get<List<MessageRequest>>(scheduledKey) ?? new List<MessageRequest>();
        
        scheduledMessages.Add(request);
        _cache.Set(scheduledKey, scheduledMessages, TimeSpan.FromDays(30));

        // Orijinal request'i retry için sakla
        var retryKey = $"retry_request:{request.RequestId}";
        _cache.Set(retryKey, request, TimeSpan.FromDays(7));

        _logger.LogDebug("Mesaj zamanlandı: {MessageId}, ScheduledAt: {ScheduledAt}", 
            request.RequestId, request.ScheduledAt);
    }
} 