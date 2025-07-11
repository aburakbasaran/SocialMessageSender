using SocialMediaMessaging.Core.Models;

namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Mesaj validasyon servisi arayüzü
/// </summary>
public interface IMessageValidator
{
    /// <summary>
    /// Mesaj isteğini validate eder
    /// </summary>
    /// <param name="request">Mesaj isteği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateAsync(MessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Platform bazında mesajı validate eder
    /// </summary>
    /// <param name="request">Mesaj isteği</param>
    /// <param name="platform">Platform adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateForPlatformAsync(MessageRequest request, string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attachment'ları validate eder
    /// </summary>
    /// <param name="attachments">Ekler</param>
    /// <param name="platform">Platform adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateAttachmentsAsync(List<Attachment> attachments, string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mesaj içeriğini validate eder
    /// </summary>
    /// <param name="content">Mesaj içeriği</param>
    /// <param name="type">Mesaj tipi</param>
    /// <param name="platform">Platform adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateContentAsync(string content, MessageType type, string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toplu mesajları validate eder
    /// </summary>
    /// <param name="requests">Mesaj istekleri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonuçları</returns>
    Task<List<ValidationResult>> ValidateBulkAsync(List<MessageRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamanlanmış mesajı validate eder
    /// </summary>
    /// <param name="request">Mesaj isteği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateScheduledMessageAsync(MessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Platform kısıtlamalarını kontrol eder
    /// </summary>
    /// <param name="request">Mesaj isteği</param>
    /// <param name="constraints">Platform kısıtlamaları</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Validasyon sonucu</returns>
    Task<ValidationResult> ValidateAgainstConstraintsAsync(MessageRequest request, PlatformConstraints constraints, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validasyon sonucu
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Validasyon başarılı mı?
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Hata mesajları
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Uyarı mesajları
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Başarılı validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Başarılı validasyon sonucu oluşturur (uyarılarla)
    /// </summary>
    public static ValidationResult SuccessWithWarnings(params string[] warnings)
    {
        return new ValidationResult 
        { 
            IsValid = true, 
            Warnings = warnings.ToList() 
        };
    }

    /// <summary>
    /// Hatalı validasyon sonucu oluşturur
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }

    /// <summary>
    /// Hatalı validasyon sonucu oluşturur (liste ile)
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }

    /// <summary>
    /// Hata ekler
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Uyarı ekler
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Başka bir validasyon sonucunu birleştirir
    /// </summary>
    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        IsValid = IsValid && other.IsValid;
    }
} 