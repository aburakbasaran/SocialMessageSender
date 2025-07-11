using FluentAssertions;
using SocialMediaMessaging.Core.Models;
using SocialMediaMessaging.Core.Enums;

namespace SocialMediaMessaging.UnitTests.Core.Models;

public class MessageRequestTests
{
    [Fact]
    public void MessageRequest_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" }
        };

        // Assert
        request.RequestId.Should().NotBeNull();
        request.Type.Should().Be(MessageType.Text);
        request.Priority.Should().Be(MessagePriority.Normal);
        request.EnableRetry.Should().BeTrue();
        request.MaxRetryAttempts.Should().Be(3);
        request.Platforms.Should().NotBeNull();
        request.Attachments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void MessageRequest_WithContent_ShouldSetContentCorrectly()
    {
        // Arrange
        var content = "Test message content";

        // Act
        var request = new MessageRequest
        {
            Content = content,
            Platforms = new List<string> { "telegram" }
        };

        // Assert
        request.Content.Should().Be(content);
    }

    [Fact]
    public void MessageRequest_WithPlatforms_ShouldSetPlatformsCorrectly()
    {
        // Arrange
        var platforms = new List<string> { "telegram", "discord", "twitter" };

        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = platforms
        };

        // Assert
        request.Platforms.Should().BeEquivalentTo(platforms);
    }

    [Fact]
    public void MessageRequest_WithAttachments_ShouldSetAttachmentsCorrectly()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new Attachment
            {
                FileName = "test.txt",
                ContentType = "text/plain",
                Data = Convert.FromBase64String("SGVsbG8gV29ybGQ="),
                Size = 11
            }
        };

        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" },
            Attachments = attachments
        };

        // Assert
        request.Attachments.Should().BeEquivalentTo(attachments);
    }

    [Theory]
    [InlineData(MessageType.Text)]
    [InlineData(MessageType.RichText)]
    [InlineData(MessageType.Image)]
    [InlineData(MessageType.Video)]
    [InlineData(MessageType.Document)]
    public void MessageRequest_WithDifferentTypes_ShouldSetTypeCorrectly(MessageType messageType)
    {
        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" },
            Type = messageType
        };

        // Assert
        request.Type.Should().Be(messageType);
    }

    [Theory]
    [InlineData(MessagePriority.Low)]
    [InlineData(MessagePriority.Normal)]
    [InlineData(MessagePriority.High)]
    public void MessageRequest_WithDifferentPriorities_ShouldSetPriorityCorrectly(MessagePriority priority)
    {
        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" },
            Priority = priority
        };

        // Assert
        request.Priority.Should().Be(priority);
    }

    [Fact]
    public void MessageRequest_WithScheduledAt_ShouldSetScheduledAtCorrectly()
    {
        // Arrange
        var scheduledAt = DateTime.UtcNow.AddHours(1);

        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" },
            ScheduledAt = scheduledAt
        };

        // Assert
        request.ScheduledAt.Should().Be(scheduledAt);
    }

    [Fact]
    public void MessageRequest_WithRetrySettings_ShouldSetRetrySettingsCorrectly()
    {
        // Act
        var request = new MessageRequest
        {
            Content = "Test content",
            Platforms = new List<string> { "telegram" },
            EnableRetry = false,
            MaxRetryAttempts = 5
        };

        // Assert
        request.EnableRetry.Should().BeFalse();
        request.MaxRetryAttempts.Should().Be(5);
    }
} 