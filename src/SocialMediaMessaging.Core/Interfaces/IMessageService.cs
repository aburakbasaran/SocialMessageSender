using SocialMediaMessaging.Core.Models;

namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Mesaj servisi arayüzü
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Tek mesaj gönderir
    /// </summary>
    /// <param name="request">Mesaj isteği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj yanıtı</returns>
    Task<MessageResponse> SendMessageAsync(MessageRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Toplu mesaj gönderir
    /// </summary>
    /// <param name="requests">Mesaj istekleri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj yanıtları</returns>
    Task<List<MessageResponse>> SendBulkMessageAsync(List<MessageRequest> requests, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mesaj durumunu sorgular
    /// </summary>
    /// <param name="messageId">Mesaj kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj yanıtı</returns>
    Task<MessageResponse?> GetMessageStatusAsync(string messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Zamanlanmış mesajları işler
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İşlenen mesaj sayısı</returns>
    Task<int> ProcessScheduledMessagesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Başarısız mesajları yeniden dener
    /// </summary>
    /// <param name="messageId">Mesaj kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yeniden deneme sonucu</returns>
    Task<MessageResponse> RetryFailedMessageAsync(string messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mesaj geçmişini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı kimliği</param>
    /// <param name="limit">Limit</param>
    /// <param name="offset">Offset</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Mesaj geçmişi</returns>
    Task<List<MessageResponse>> GetMessageHistoryAsync(string? userId = null, int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mesaj istatistiklerini getirir
    /// </summary>
    /// <param name="from">Başlangıç tarihi</param>
    /// <param name="to">Bitiş tarihi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İstatistikler</returns>
    Task<Dictionary<string, object>> GetMessageStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
} 