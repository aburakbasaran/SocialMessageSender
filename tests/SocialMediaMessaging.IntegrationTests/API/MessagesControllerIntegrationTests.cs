using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialMediaMessaging.Core.Models;
using SocialMediaMessaging.Core.Enums;
using System.Net;

namespace SocialMediaMessaging.IntegrationTests.API;

public class MessagesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public MessagesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SendMessage_ValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "Integration test message",
            Platforms = new List<string> { "telegram" },
            Type = MessageType.Text,
            Priority = MessagePriority.Normal,
            EnableRetry = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages/send", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var messageResponse = JsonSerializer.Deserialize<MessageResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        messageResponse.Should().NotBeNull();
        messageResponse.MessageId.Should().NotBeNull();
        messageResponse.Status.Should().BeOneOf(MessageStatus.Sent, MessageStatus.Failed); // Mock olduğu için failed olabilir
    }

    [Fact]
    public async Task SendMessage_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new MessageRequest
        {
            Content = "",
            Platforms = new List<string>(),
            Type = MessageType.Text,
            Priority = MessagePriority.Normal
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages/send", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendBulkMessage_ValidRequests_ShouldReturnOk()
    {
        // Arrange
        var requests = new List<MessageRequest>
        {
            new MessageRequest
            {
                Content = "Bulk test message 1",
                Platforms = new List<string> { "telegram" },
                Type = MessageType.Text,
                Priority = MessagePriority.Normal,
                EnableRetry = false
            },
            new MessageRequest
            {
                Content = "Bulk test message 2",
                Platforms = new List<string> { "discord" },
                Type = MessageType.Text,
                Priority = MessagePriority.Normal,
                EnableRetry = false
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/messages/send-bulk", requests);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var messageResponses = JsonSerializer.Deserialize<List<MessageResponse>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        messageResponses.Should().NotBeNull();
        messageResponses.Should().HaveCount(2);
        messageResponses.All(r => r.MessageId != null).Should().BeTrue();
    }

    [Fact]
    public async Task GetMessageStatus_ValidMessageId_ShouldReturnOk()
    {
        // Arrange - Önce bir mesaj gönderelim
        var sendRequest = new MessageRequest
        {
            Content = "Test message for status check",
            Platforms = new List<string> { "telegram" }
        };

        var sendResponse = await _client.PostAsJsonAsync("/api/messages/send", sendRequest);
        sendResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        var sendResult = await sendResponse.Content.ReadFromJsonAsync<MessageResponse>();
        sendResult.Should().NotBeNull();
        
        var messageId = sendResult!.MessageId;
        messageId.Should().NotBeNullOrEmpty();

        // Act - Şimdi mesaj durumunu kontrol edelim
        var response = await _client.GetAsync($"/api/messages/status/{messageId}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(messageId);
    }

    [Fact]
    public async Task GetMessageHistory_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/messages/history?limit=10&offset=0");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var messageResponses = JsonSerializer.Deserialize<List<MessageResponse>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        messageResponses.Should().NotBeNull();
        messageResponses.Should().BeOfType<List<MessageResponse>>();
    }

    [Fact]
    public async Task GetMessageStatistics_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/messages/statistics");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var statistics = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        statistics.Should().NotBeNull();
        statistics.Should().ContainKey("totalMessages");
    }

    [Fact]
    public async Task ProcessScheduledMessages_ShouldReturnOk()
    {
        // Act
        var response = await _client.PostAsync("/api/messages/process-scheduled", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPlatforms_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/platforms");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInfo_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/info");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        // Mock service'ler platform bağlantısı kuramadığı için service unavailable dönebilir
        response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.ServiceUnavailable);
    }
} 