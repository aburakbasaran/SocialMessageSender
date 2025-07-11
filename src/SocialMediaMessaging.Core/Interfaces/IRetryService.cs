namespace SocialMediaMessaging.Core.Interfaces;

/// <summary>
/// Yeniden deneme servisi arayüzü
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Verilen fonksiyonu yeniden deneme ile çalıştırır
    /// </summary>
    /// <typeparam name="T">Dönüş tipi</typeparam>
    /// <param name="operation">Çalıştırılacak operasyon</param>
    /// <param name="maxAttempts">Maksimum deneme sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Operasyon sonucu</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen fonksiyonu yeniden deneme ile çalıştırır (delay ile)
    /// </summary>
    /// <typeparam name="T">Dönüş tipi</typeparam>
    /// <param name="operation">Çalıştırılacak operasyon</param>
    /// <param name="maxAttempts">Maksimum deneme sayısı</param>
    /// <param name="delay">Denemeler arası bekleme süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Operasyon sonucu</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        TimeSpan delay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exponential backoff ile yeniden deneme
    /// </summary>
    /// <typeparam name="T">Dönüş tipi</typeparam>
    /// <param name="operation">Çalıştırılacak operasyon</param>
    /// <param name="maxAttempts">Maksimum deneme sayısı</param>
    /// <param name="baseDelay">Temel bekleme süresi</param>
    /// <param name="maxDelay">Maksimum bekleme süresi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Operasyon sonucu</returns>
    Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Koşullu yeniden deneme
    /// </summary>
    /// <typeparam name="T">Dönüş tipi</typeparam>
    /// <param name="operation">Çalıştırılacak operasyon</param>
    /// <param name="maxAttempts">Maksimum deneme sayısı</param>
    /// <param name="shouldRetry">Yeniden deneme koşulu</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Operasyon sonucu</returns>
    Task<T> ExecuteWithConditionalRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeniden deneme politikasını yapılandırır
    /// </summary>
    /// <param name="policy">Retry politikası</param>
    void ConfigureRetryPolicy(RetryPolicy policy);

    /// <summary>
    /// Mevcut retry istatistiklerini getirir
    /// </summary>
    /// <returns>Retry istatistikleri</returns>
    RetryStatistics GetRetryStatistics();
}

/// <summary>
/// Yeniden deneme politikası
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Varsayılan maksimum deneme sayısı
    /// </summary>
    public int DefaultMaxAttempts { get; init; } = 3;

    /// <summary>
    /// Varsayılan bekleme süresi
    /// </summary>
    public TimeSpan DefaultDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Exponential backoff kullanılsın mı?
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Maksimum bekleme süresi
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Jitter kullanılsın mı? (rastgele gecikme)
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Yeniden denenecek exception türleri
    /// </summary>
    public List<Type> RetryableExceptions { get; init; } = new();

    /// <summary>
    /// Yeniden denenmeyecek exception türleri
    /// </summary>
    public List<Type> NonRetryableExceptions { get; init; } = new();
}

/// <summary>
/// Yeniden deneme istatistikleri
/// </summary>
public record RetryStatistics
{
    /// <summary>
    /// Toplam deneme sayısı
    /// </summary>
    public long TotalAttempts { get; init; }

    /// <summary>
    /// Başarılı işlem sayısı
    /// </summary>
    public long SuccessfulOperations { get; init; }

    /// <summary>
    /// Başarısız işlem sayısı
    /// </summary>
    public long FailedOperations { get; init; }

    /// <summary>
    /// Ortalama deneme sayısı
    /// </summary>
    public double AverageAttempts { get; init; }

    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Başarı oranı
    /// </summary>
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulOperations / TotalAttempts * 100 : 0;
} 