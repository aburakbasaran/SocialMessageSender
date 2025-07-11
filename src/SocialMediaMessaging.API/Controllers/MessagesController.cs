using Microsoft.AspNetCore.Http;

namespace SocialMediaMessaging.API.Controllers;

/// <summary>
/// Mesaj gönderim controller'ı
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;

    /// <summary>
    /// Yapıcı metod
    /// </summary>
    public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Tek mesaj gönderir
    /// </summary>
    /// <param name="request">Mesaj gönderim isteği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj gönderim sonucu</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessageResponse>> SendMessageAsync(
        [FromBody] MessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Mesaj gönderim isteği alındı: {RequestId}, Platforms: {Platforms}",
                request.RequestId, string.Join(", ", request.Platforms));

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Geçersiz model durumu: {RequestId}", request.RequestId);
                return BadRequest(ModelState);
            }

            var response = await _messageService.SendMessageAsync(request, cancellationToken);

            var statusCode = response.Status switch
            {
                MessageStatus.Sent => StatusCodes.Status200OK,
                MessageStatus.PartialSuccess => StatusCodes.Status200OK,
                MessageStatus.Pending => StatusCodes.Status202Accepted,
                MessageStatus.Failed => StatusCodes.Status200OK, // API başarılı ama mesaj gönderimi başarısız
                _ => StatusCodes.Status200OK
            };

            _logger.LogInformation("Mesaj gönderim isteği tamamlandı: {RequestId}, Status: {Status}",
                request.RequestId, response.Status);

            return StatusCode(statusCode, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Geçersiz argüman: {RequestId}", request.RequestId);
            return BadRequest(new ProblemDetails
            {
                Title = "Geçersiz İstek",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj gönderim hatası: {RequestId}", request.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesaj gönderilirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Toplu mesaj gönderir
    /// </summary>
    /// <param name="requests">Mesaj gönderim istekleri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj gönderim sonuçları</returns>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MessageResponse>>> SendBulkMessageAsync(
        [FromBody] List<MessageRequest> requests,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Toplu mesaj gönderim isteği alındı: {Count} mesaj", requests.Count);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Geçersiz model durumu");
                return BadRequest(ModelState);
            }

            if (requests.Count == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "En az bir mesaj gönderilmelidir.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (requests.Count > 100) // Toplu mesaj limiti
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Tek seferde maksimum 100 mesaj gönderilebilir.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var responses = await _messageService.SendBulkMessageAsync(requests, cancellationToken);

            var successCount = responses.Count(r => r.Status == MessageStatus.Sent || r.Status == MessageStatus.PartialSuccess);

            _logger.LogInformation("Toplu mesaj gönderim isteği tamamlandı: {Success}/{Total} başarılı",
                successCount, requests.Count);

            return Ok(responses);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Geçersiz argüman");
            return BadRequest(new ProblemDetails
            {
                Title = "Geçersiz İstek",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu mesaj gönderim hatası");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesajlar gönderilirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Mesaj durumunu sorgular
    /// </summary>
    /// <param name="messageId">Mesaj kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj durumu</returns>
    [HttpGet("status/{messageId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessageResponse>> GetMessageStatusAsync(
        [FromRoute] string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mesaj durumu sorgulanıyor: {MessageId}", messageId);

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Mesaj kimliği boş olamaz.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = await _messageService.GetMessageStatusAsync(messageId, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Mesaj bulunamadı: {MessageId}", messageId);
                return NotFound(new ProblemDetails
                {
                    Title = "Mesaj Bulunamadı",
                    Detail = $"'{messageId}' kimlikli mesaj bulunamadı.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogDebug("Mesaj durumu bulundu: {MessageId}, Status: {Status}", messageId, response.Status);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj durumu sorgulama hatası: {MessageId}", messageId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesaj durumu sorgulanırken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Başarısız mesajı yeniden dener
    /// </summary>
    /// <param name="messageId">Mesaj kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yeniden deneme sonucu</returns>
    [HttpPost("retry/{messageId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MessageResponse>> RetryFailedMessageAsync(
        [FromRoute] string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Mesaj yeniden deneniyor: {MessageId}", messageId);

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Mesaj kimliği boş olamaz.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = await _messageService.RetryFailedMessageAsync(messageId, cancellationToken);

            if (response.Errors.Any(e => e.Contains("bulunamadı")))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Mesaj Bulunamadı",
                    Detail = $"'{messageId}' kimlikli mesaj bulunamadı.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Mesaj yeniden deneme tamamlandı: {MessageId}, Status: {Status}", 
                messageId, response.Status);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj yeniden deneme hatası: {MessageId}", messageId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesaj yeniden denenirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Mesaj geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı kimliği (isteğe bağlı)</param>
    /// <param name="limit">Limit (varsayılan: 100, maksimum: 1000)</param>
    /// <param name="offset">Offset (varsayılan: 0)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj geçmişi</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<MessageResponse>>> GetMessageHistoryAsync(
        [FromQuery] string? userId = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mesaj geçmişi getiriliyor: UserId: {UserId}, Limit: {Limit}, Offset: {Offset}",
                userId, limit, offset);

            if (limit <= 0 || limit > 1000)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Limit 1 ile 1000 arasında olmalıdır.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (offset < 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Offset 0'dan küçük olamaz.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var history = await _messageService.GetMessageHistoryAsync(userId, limit, offset, cancellationToken);

            _logger.LogDebug("Mesaj geçmişi getirildi: {Count} mesaj", history.Count);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj geçmişi getirme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesaj geçmişi getirilirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Mesaj istatistiklerini getirir
    /// </summary>
    /// <param name="from">Başlangıç tarihi (isteğe bağlı)</param>
    /// <param name="to">Bitiş tarihi (isteğe bağlı)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj istatistikleri</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dictionary<string, object>>> GetMessageStatisticsAsync(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mesaj istatistikleri getiriliyor: From: {From}, To: {To}", from, to);

            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Geçersiz İstek",
                    Detail = "Başlangıç tarihi bitiş tarihinden büyük olamaz.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var statistics = await _messageService.GetMessageStatisticsAsync(from, to, cancellationToken);

            _logger.LogDebug("Mesaj istatistikleri getirildi");
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj istatistikleri getirme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Mesaj istatistikleri getirilirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Zamanlanmış mesajları işler (manuel tetikleme için)
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İşlenen mesaj sayısı</returns>
    [HttpPost("process-scheduled")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> ProcessScheduledMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Zamanlanmış mesajlar manuel olarak işleniyor");

            var processedCount = await _messageService.ProcessScheduledMessagesAsync(cancellationToken);

            _logger.LogInformation("Zamanlanmış mesaj işleme tamamlandı: {ProcessedCount} mesaj işlendi", processedCount);

            return Ok(new { processedCount, processedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış mesaj işleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Zamanlanmış mesajlar işlenirken bir hata oluştu.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
} 