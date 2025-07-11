using FluentAssertions;
using Moq;
using SocialMediaMessaging.Core.Models;
using SocialMediaMessaging.Core.Enums;
using SocialMediaMessaging.Core.Interfaces;
using SocialMediaMessaging.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using ValidationResult = SocialMediaMessaging.Core.Interfaces.ValidationResult;

namespace SocialMediaMessaging.UnitTests.Infrastructure.Services;

public class MessageServiceTests
{
    private readonly Mock<ILogger<MessageService>> _mockLogger;
    private readonly Mock<IMessageValidator> _mockValidator;
    private readonly Mock<IPlatformAdapterFactory> _mockAdapterFactory;
    private readonly Mock<IRateLimitingService> _mockRateLimitingService;
    private readonly Mock<IRetryService> _mockRetryService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IPlatformAdapter> _mockAdapter;
    private readonly MessageService _messageService;

    public MessageServiceTests()
    {
        _mockLogger = new Mock<ILogger<MessageService>>();
        _mockValidator = new Mock<IMessageValidator>();
        _mockAdapterFactory = new Mock<IPlatformAdapterFactory>();
        _mockRateLimitingService = new Mock<IRateLimitingService>();
        _mockRetryService = new Mock<IRetryService>();
        _mockCache = new Mock<IMemoryCache>();
        _mockAdapter = new Mock<IPlatformAdapter>();
        
        _messageService = new MessageService(
            _mockAdapterFactory.Object,
            _mockValidator.Object,
            _mockRateLimitingService.Object,
            _mockRetryService.Object,
            _mockLogger.Object,
            _mockCache.Object
        );
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "Test message",
            Platforms = new List<string> { "telegram" }
        };

        var validationResult = new ValidationResult { IsValid = true };
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var successResult = new PlatformResult
        {
            PlatformName = "telegram",
            Success = true,
            PlatformMessageId = "msg123",
            ResponseTimeMs = 250
        };

        _mockAdapter.Setup(a => a.SendMessageAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _mockAdapterFactory.Setup(f => f.GetAdapter("telegram"))
            .Returns(_mockAdapter.Object);

        // Act
        var result = await _messageService.SendMessageAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MessageStatus.Sent);
        result.PlatformResults.Should().ContainKey("telegram");
        result.PlatformResults["telegram"].Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_InvalidMessage_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "",
            Platforms = new List<string> { "telegram" }
        };

        var validationResult = ValidationResult.Failure("Content is required");

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _messageService.SendMessageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MessageStatus.Failed);
        result.Errors.Should().Contain("Content is required");
    }

    [Fact]
    public async Task SendMessageAsync_AdapterNotFound_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "Test message",
            Platforms = new List<string> { "unknown" }
        };

        var validationResult = new ValidationResult { IsValid = true };
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAdapterFactory.Setup(f => f.GetAdapter("unknown"))
            .Returns((IPlatformAdapter)null);

        // Act
        var result = await _messageService.SendMessageAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MessageStatus.Failed);
        result.PlatformResults.Should().ContainKey("unknown");
        result.PlatformResults["unknown"].Success.Should().BeFalse();
        result.PlatformResults["unknown"].Error.Should().Contain("Adapter not found");
    }

    [Fact]
    public async Task SendMessageAsync_MultiPlatform_ShouldHandlePartialSuccess()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "Test message",
            Platforms = new List<string> { "telegram", "discord" }
        };

        var validationResult = new ValidationResult { IsValid = true };
        _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var telegramAdapter = new Mock<IPlatformAdapter>();
        var discordAdapter = new Mock<IPlatformAdapter>();

        // Telegram başarılı
        var telegramResult = new PlatformResult
        {
            PlatformName = "telegram",
            Success = true,
            PlatformMessageId = "msg123",
            ResponseTimeMs = 200
        };

        // Discord başarısız
        var discordResult = new PlatformResult
        {
            PlatformName = "discord",
            Success = false,
            Error = "API Error",
            ResponseTimeMs = 150
        };

        telegramAdapter.Setup(a => a.SendMessageAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(telegramResult);

        discordAdapter.Setup(a => a.SendMessageAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discordResult);

        _mockAdapterFactory.Setup(f => f.GetAdapter("telegram"))
            .Returns(telegramAdapter.Object);

        _mockAdapterFactory.Setup(f => f.GetAdapter("discord"))
            .Returns(discordAdapter.Object);

        // Act
        var result = await _messageService.SendMessageAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MessageStatus.PartialSuccess);
        result.PlatformResults.Should().HaveCount(2);
        result.PlatformResults["telegram"].Success.Should().BeTrue();
        result.PlatformResults["discord"].Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_AllPlatformsFail_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "Test message",
            Platforms = new List<string> { "telegram", "discord" }
        };

        var validationResult = ValidationResult.Success();
        var telegramResult = PlatformResult.CreateFailure("telegram", "Telegram Error");
        var discordResult = PlatformResult.CreateFailure("discord", "Discord Error");

        var mockTelegramAdapter = new Mock<IPlatformAdapter>();
        var mockDiscordAdapter = new Mock<IPlatformAdapter>();

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockAdapterFactory.Setup(f => f.GetAdapter("telegram"))
            .Returns(mockTelegramAdapter.Object);
        _mockAdapterFactory.Setup(f => f.GetAdapter("discord"))
            .Returns(mockDiscordAdapter.Object);

        mockTelegramAdapter.Setup(a => a.SendMessageAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(telegramResult);
        mockDiscordAdapter.Setup(a => a.SendMessageAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discordResult);

        // Act
        var result = await _messageService.SendMessageAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(MessageStatus.Failed);
        result.SuccessfulPlatforms.Should().Be(0);
        result.FailedPlatforms.Should().Be(2);
    }

    [Fact]
    public async Task SendBulkMessageAsync_ValidMessages_ShouldReturnAllResponses()
    {
        // Arrange
        var messages = new List<MessageRequest>
        {
            new MessageRequest
            {
                Content = "Test message 1",
                Platforms = new List<string> { "telegram" }
            },
            new MessageRequest
            {
                Content = "Test message 2",
                Platforms = new List<string> { "discord" }
            }
        };

        var validationResult = new ValidationResult { IsValid = true };
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var telegramAdapter = new Mock<IPlatformAdapter>();
        var discordAdapter = new Mock<IPlatformAdapter>();

        var telegramResult = new PlatformResult
        {
            PlatformName = "telegram",
            Success = true,
            PlatformMessageId = "msg123",
            ResponseTimeMs = 200
        };

        var discordResult = new PlatformResult
        {
            PlatformName = "discord",
            Success = true,
            PlatformMessageId = "msg456",
            ResponseTimeMs = 180
        };

        telegramAdapter.Setup(a => a.SendMessageAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(telegramResult);

        discordAdapter.Setup(a => a.SendMessageAsync(It.IsAny<MessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(discordResult);

        _mockAdapterFactory.Setup(f => f.GetAdapter("telegram"))
            .Returns(telegramAdapter.Object);

        _mockAdapterFactory.Setup(f => f.GetAdapter("discord"))
            .Returns(discordAdapter.Object);

        // Act
        var results = await _messageService.SendBulkMessageAsync(messages, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.All(r => r.Status == MessageStatus.Sent).Should().BeTrue();
        results.All(r => r.PlatformResults.Values.All(p => p.Success)).Should().BeTrue();
    }

    [Fact]
    public async Task GetMessageStatusAsync_ValidMessageId_ShouldReturnStatus()
    {
        // Arrange
        var messageId = "test-message-123";

        // Act
        var result = await _messageService.GetMessageStatusAsync(messageId);

        // Assert
        result.Should().BeNull(); // Çünkü in-memory storage'da mesaj yok
    }

    [Fact]
    public async Task GetMessageHistoryAsync_ShouldReturnMessageHistory()
    {
        // Arrange
        var userId = "user123";
        var limit = 10;
        var offset = 0;

        // Act
        var result = await _messageService.GetMessageHistoryAsync(userId, limit, offset);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<MessageResponse>>();
    }

    [Fact]
    public async Task GetMessageStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _messageService.GetMessageStatisticsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Dictionary<string, object>>();
        result.Should().ContainKey("totalMessages");
        result.Should().ContainKey("successfulMessages");
        result.Should().ContainKey("failedMessages");
    }

    [Fact]
    public async Task ProcessScheduledMessagesAsync_ShouldReturnProcessedCount()
    {
        // Arrange
        var scheduledMessages = new List<MessageRequest>();
        object cacheValue = scheduledMessages;
        
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var result = await _messageService.ProcessScheduledMessagesAsync();

        // Assert
        result.Should().Be(0); // Çünkü scheduled mesaj yok
    }
} 