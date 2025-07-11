using FluentAssertions;
using SocialMediaMessaging.Core.Models;
using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.UnitTests.Core.Models;

public class MessageResponseTests
{
    [Fact]
    public void MessageResponse_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id"
        };

        // Assert
        response.MessageId.Should().Be("test-message-id");
        response.Status.Should().Be(MessageStatus.Pending);
        response.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        response.PlatformResults.Should().NotBeNull().And.BeEmpty();
        response.Errors.Should().NotBeNull().And.BeEmpty();
        response.TotalPlatforms.Should().Be(0);
        response.SuccessfulPlatforms.Should().Be(0);
        response.FailedPlatforms.Should().Be(0);
    }

    [Fact]
    public void MessageResponse_WithMessageId_ShouldSetMessageIdCorrectly()
    {
        // Arrange
        var messageId = "test-message-123";

        // Act
        var response = new MessageResponse
        {
            MessageId = messageId
        };

        // Assert
        response.MessageId.Should().Be(messageId);
    }

    [Theory]
    [InlineData(MessageStatus.Pending)]
    [InlineData(MessageStatus.Sent)]
    [InlineData(MessageStatus.Failed)]
    [InlineData(MessageStatus.Retry)]
    [InlineData(MessageStatus.PartialSuccess)]
    [InlineData(MessageStatus.Cancelled)]
    public void MessageResponse_WithDifferentStatuses_ShouldSetStatusCorrectly(MessageStatus status)
    {
        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            Status = status
        };

        // Assert
        response.Status.Should().Be(status);
    }

    [Fact]
    public void MessageResponse_WithSentAt_ShouldSetSentAtCorrectly()
    {
        // Arrange
        var sentAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            SentAt = sentAt
        };

        // Assert
        response.SentAt.Should().Be(sentAt);
    }

    [Fact]
    public void MessageResponse_WithPlatformResults_ShouldSetPlatformResultsCorrectly()
    {
        // Arrange
        var platformResults = new Dictionary<string, PlatformResult>
        {
            {
                "telegram", new PlatformResult
                {
                    PlatformName = "telegram",
                    Success = true,
                    PlatformMessageId = "msg123",
                    ResponseTimeMs = 250
                }
            },
            {
                "discord", new PlatformResult
                {
                    PlatformName = "discord",
                    Success = false,
                    Error = "API Error",
                    ResponseTimeMs = 150
                }
            }
        };

        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            PlatformResults = platformResults
        };

        // Assert
        response.PlatformResults.Should().BeEquivalentTo(platformResults);
    }

    [Fact]
    public void MessageResponse_WithErrors_ShouldSetErrorsCorrectly()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };

        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            Errors = errors
        };

        // Assert
        response.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void MessageResponse_WithPlatformCounts_ShouldCalculateCountsCorrectly()
    {
        // Arrange
        var platformResults = new Dictionary<string, PlatformResult>
        {
            { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } },
            { "discord", new PlatformResult { PlatformName = "discord", Success = false } },
            { "twitter", new PlatformResult { PlatformName = "twitter", Success = true } }
        };

        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            PlatformResults = platformResults
        };

        // Assert
        response.TotalPlatforms.Should().Be(3);
        response.SuccessfulPlatforms.Should().Be(2);
        response.FailedPlatforms.Should().Be(1);
    }

    [Fact]
    public void MessageResponse_WithProcessingTime_ShouldSetProcessingTimeCorrectly()
    {
        // Arrange
        var processingTimeMs = 500L;

        // Act
        var response = new MessageResponse
        {
            MessageId = "test-message-id",
            ProcessingTimeMs = processingTimeMs
        };

        // Assert
        response.ProcessingTimeMs.Should().Be(processingTimeMs);
    }

    [Fact]
    public void MessageResponse_CreateSuccess_ShouldCreateSuccessResponse()
    {
        // Arrange
        var messageId = "success-message-id";
        var platformResults = new Dictionary<string, PlatformResult>
        {
            { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } }
        };

        // Act
        var response = MessageResponse.CreateSuccess(messageId, platformResults);

        // Assert
        response.MessageId.Should().Be(messageId);
        response.Status.Should().Be(MessageStatus.Sent);
        response.PlatformResults.Should().BeEquivalentTo(platformResults);
    }

    [Fact]
    public void MessageResponse_CreateFailure_ShouldCreateFailureResponse()
    {
        // Arrange
        var messageId = "failure-message-id";
        var errors = new List<string> { "Test error" };

        // Act
        var response = MessageResponse.CreateFailure(messageId, errors);

        // Assert
        response.MessageId.Should().Be(messageId);
        response.Status.Should().Be(MessageStatus.Failed);
        response.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void MessageResponse_CreatePartialSuccess_ShouldCreatePartialSuccessResponse()
    {
        // Arrange
        var messageId = "partial-message-id";
        var platformResults = new Dictionary<string, PlatformResult>
        {
            { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } },
            { "discord", new PlatformResult { PlatformName = "discord", Success = false } }
        };

        // Act
        var response = MessageResponse.CreatePartialSuccess(messageId, platformResults);

        // Assert
        response.MessageId.Should().Be(messageId);
        response.Status.Should().Be(MessageStatus.PartialSuccess);
        response.PlatformResults.Should().BeEquivalentTo(platformResults);
    }

    [Fact]
    public void MessageResponse_IsCompletelySuccessful_ShouldReturnTrueWhenAllPlatformsSucceed()
    {
        // Arrange
        var response = new MessageResponse
        {
            MessageId = "test-id",
            PlatformResults = new Dictionary<string, PlatformResult>
            {
                { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } },
                { "discord", new PlatformResult { PlatformName = "discord", Success = true } }
            }
        };

        // Act & Assert
        response.IsCompletelySuccessful.Should().BeTrue();
    }

    [Fact]
    public void MessageResponse_IsCompletelyFailed_ShouldReturnTrueWhenAllPlatformsFail()
    {
        // Arrange
        var response = new MessageResponse
        {
            MessageId = "test-id",
            PlatformResults = new Dictionary<string, PlatformResult>
            {
                { "telegram", new PlatformResult { PlatformName = "telegram", Success = false } },
                { "discord", new PlatformResult { PlatformName = "discord", Success = false } }
            }
        };

        // Act & Assert
        response.IsCompletelyFailed.Should().BeTrue();
    }

    [Fact]
    public void MessageResponse_IsPartiallySuccessful_ShouldReturnTrueWhenSomePlatformsSucceed()
    {
        // Arrange
        var response = new MessageResponse
        {
            MessageId = "test-id",
            PlatformResults = new Dictionary<string, PlatformResult>
            {
                { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } },
                { "discord", new PlatformResult { PlatformName = "discord", Success = false } }
            }
        };

        // Act & Assert
        response.IsPartiallySuccessful.Should().BeTrue();
    }

    [Fact]
    public void MessageResponse_SuccessRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var response = new MessageResponse
        {
            MessageId = "test-id",
            PlatformResults = new Dictionary<string, PlatformResult>
            {
                { "telegram", new PlatformResult { PlatformName = "telegram", Success = true } },
                { "discord", new PlatformResult { PlatformName = "discord", Success = false } },
                { "twitter", new PlatformResult { PlatformName = "twitter", Success = true } }
            }
        };

        // Act & Assert
        response.SuccessRate.Should().BeApproximately(0.666, 0.001);
    }
} 