using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class ReportsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReportsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetWeeklyReport_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/reports/weekly?weekStart=2024-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeeklyReport_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var weekStart = new DateTime(2024, 1, 1);

        // Criar alguns check-ins para a semana
        await CreateCheckinsForWeek(token, weekStart, 5);

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/weekly?weekStart={weekStart:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<WeeklyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.WeekStart.Should().Be(weekStart);
        apiResponse.Data.Averages.Should().NotBeNull();
        apiResponse.Data.DailyData.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeeklyReport_WithoutWeekStart_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/weekly");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeeklyReport_WithDifferentUserId_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var differentUserId = Guid.NewGuid();
        var weekStart = new DateTime(2024, 1, 1);

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/weekly?weekStart={weekStart:yyyy-MM-dd}&userId={differentUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMonthlyReport_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=2024&month=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMonthlyReport_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var year = 2024;
        var month = 11;

        // Criar alguns check-ins para o mês
        await CreateCheckinsForMonth(token, year, month, 10);

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/monthly?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<MonthlyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Year.Should().Be(year);
        apiResponse.Data.Month.Should().Be(month);
        apiResponse.Data.Averages.Should().NotBeNull();
        apiResponse.Data.WeeklySummaries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMonthlyReport_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=2024&month=13");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMonthlyReport_WithMonthZero_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=2024&month=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMonthlyReport_WithoutCheckins_ReturnsEmptyReport()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var year = 2024;
        var month = 1;

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/monthly?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<MonthlyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.TotalCheckins.Should().Be(0);
    }

    [Fact]
    public async Task GetWeeklyReport_WithCheckins_ReturnsCorrectData()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        // Criar check-ins com valores conhecidos
        for (int i = 0; i < 7; i++)
        {
            var checkinDto = new CreateCheckinDto
            {
                Mood = 4,
                Stress = 2,
                Sleep = 5
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", checkinDto);
            await Task.Delay(100);
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/weekly?weekStart={weekStart:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<WeeklyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Averages.Should().NotBeNull();
        apiResponse.Data.Averages.Mood.Should().BeGreaterThan(0);
        apiResponse.Data.Averages.Stress.Should().BeGreaterThan(0);
        apiResponse.Data.Averages.Sleep.Should().BeGreaterThan(0);
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

    private async Task CreateCheckinsForWeek(string token, DateTime weekStart, int count)
    {
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < count && i < 7; i++)
        {
            var checkinDto = new CreateCheckinDto
            {
                Mood = 4,
                Stress = 2,
                Sleep = 5
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", checkinDto);
            await Task.Delay(100);
        }
    }

    private async Task CreateCheckinsForMonth(string token, int year, int month, int count)
    {
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var checkinsPerDay = Math.Max(1, count / daysInMonth);

        for (int day = 1; day <= Math.Min(daysInMonth, count); day++)
        {
            var checkinDto = new CreateCheckinDto
            {
                Mood = 3 + (day % 3),
                Stress = 2 + (day % 2),
                Sleep = 4 + (day % 2)
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", checkinDto);
            await Task.Delay(50);
        }
    }
}

