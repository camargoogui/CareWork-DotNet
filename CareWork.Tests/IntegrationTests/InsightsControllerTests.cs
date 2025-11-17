using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class InsightsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public InsightsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetTrends_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/insights/trends");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTrends_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar alguns check-ins primeiro
        await CreateCheckinsForUser(token, 5);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/trends?period=week");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TrendsInsightDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Period.Should().Be("week");
    }

    [Fact]
    public async Task GetTrends_WithMonthPeriod_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await CreateCheckinsForUser(token, 3);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/trends?period=month");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TrendsInsightDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Period.Should().Be("month");
    }

    [Fact]
    public async Task GetStreak_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar check-ins consecutivos
        await CreateCheckinsForUser(token, 3);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/streak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<StreakDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.CurrentStreak.Should().BeGreaterThanOrEqualTo(0);
        apiResponse.Data.LongestStreak.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetStreak_WithoutCheckins_ReturnsZero()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/streak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<StreakDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.CurrentStreak.Should().Be(0);
        apiResponse.Data.LongestStreak.Should().Be(0);
    }

    [Fact]
    public async Task ComparePeriods_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await CreateCheckinsForUser(token, 5);

        var start1 = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-dd");
        var end1 = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var start2 = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var end2 = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/v1/insights/compare?start1={start1}&end1={end1}&start2={start2}&end2={end2}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<ComparisonDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Period1.Should().NotBeNull();
        apiResponse.Data.Period2.Should().NotBeNull();
        apiResponse.Data.Comparison.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendedTips_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar um check-in com stress alto
        var checkinDto = new CreateCheckinDto
        {
            Mood = 2,
            Stress = 5, // Alto stress
            Sleep = 3
        };
        await _client.PostAsJsonAsync("/api/v1/checkins", checkinDto);

        // Criar algumas tips
        var stressTip = new CreateTipDto
        {
            Title = "Stress Management",
            Description = "How to manage stress",
            Category = "Stress"
        };
        await _client.PostAsJsonAsync("/api/v1/tips", stressTip);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/recommended-tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<TipDto>>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecommendedTips_WithoutCheckins_ReturnsGeneralTips()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar algumas tips gerais
        var wellnessTip = new CreateTipDto
        {
            Title = "Wellness Tip",
            Description = "General wellness tip",
            Category = "Wellness"
        };
        await _client.PostAsJsonAsync("/api/v1/tips", wellnessTip);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/recommended-tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<TipDto>>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var registerDto = new RegisterDto
        {
            Email = $"test_{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            Name = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        
        return apiResponse?.Data?.Token ?? throw new Exception("Failed to get token");
    }

    private async Task CreateCheckinsForUser(string token, int count)
    {
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < count; i++)
        {
            var checkinDto = new CreateCheckinDto
            {
                Mood = 3 + (i % 3), // Varia entre 3 e 5
                Stress = 2 + (i % 2), // Varia entre 2 e 3
                Sleep = 4 + (i % 2) // Varia entre 4 e 5
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", checkinDto);
            await Task.Delay(100); // Pequeno delay para garantir diferentes timestamps
        }
    }
}

