using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

/// <summary>
/// Testes completos para TODOS os endpoints da API
/// Garante cobertura completa de todos os métodos HTTP
/// </summary>
public class AllEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AllEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Auth Endpoints

    [Fact]
    public async Task Auth_Register_Endpoint_Works()
    {
        // Act
        var registerDto = new RegisterDto
        {
            Email = $"test_{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            Name = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Auth_Login_Endpoint_Works()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@test.com";
        var password = "Test123!";

        await RegisterUser(email, password);

        // Act
        var loginDto = new LoginDto { Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Auth_UpdateProfile_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Email = $"updated_{Guid.NewGuid()}@test.com"
        };
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<UserDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Auth_UpdatePassword_Endpoint_Works()
    {
        // Arrange
        var password = "Test123!";
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = password,
            NewPassword = "NewPassword123!"
        };
        var response = await _client.PutAsJsonAsync("/api/v1/auth/password", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auth_DeleteAccount_Endpoint_Works()
    {
        // Arrange
        var password = "Test123!";
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var deleteDto = new DeleteAccountDto { Password = password };
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Checkins Endpoints

    [Fact]
    public async Task Checkins_GetAll_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Checkins_GetById_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar check-in
        var createDto = new CreateCheckinDto { Mood = 4, Stress = 2, Sleep = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(createContent, _jsonOptions);
        var checkinId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/checkins/{checkinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse!.Data!.Id.Should().Be(checkinId);
    }

    [Fact]
    public async Task Checkins_Create_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var createDto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            Notes = "Test notes",
            Tags = new List<string> { "test", "work" }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse!.Data!.Mood.Should().Be(4);
        apiResponse.Data.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task Checkins_Update_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar check-in
        var createDto = new CreateCheckinDto { Mood = 3, Stress = 3, Sleep = 3 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(createContent, _jsonOptions);
        var checkinId = createApiResponse!.Data!.Id;

        // Act
        var updateDto = new UpdateCheckinDto
        {
            Mood = 5,
            Stress = 1,
            Sleep = 5,
            Notes = "Updated notes"
        };
        var response = await _client.PutAsJsonAsync($"/api/v1/checkins/{checkinId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse!.Data!.Mood.Should().Be(5);
        apiResponse.Data.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task Checkins_Delete_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar check-in
        var createDto = new CreateCheckinDto { Mood = 4, Stress = 2, Sleep = 5 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(createContent, _jsonOptions);
        var checkinId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/v1/checkins/{checkinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que foi deletado
        var getResponse = await _client.GetAsync($"/api/v1/checkins/{checkinId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Tips Endpoints

    [Fact]
    public async Task Tips_GetAll_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/tips?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<TipDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
    }

    [Fact]
    public async Task Tips_GetById_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar tip
        var createDto = new CreateTipDto
        {
            Title = "Test Tip",
            Description = "Test Description",
            Category = "Wellness"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(createContent, _jsonOptions);
        var tipId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/tips/{tipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(content, _jsonOptions);
        apiResponse!.Data!.Id.Should().Be(tipId);
    }

    [Fact]
    public async Task Tips_Create_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var createDto = new CreateTipDto
        {
            Title = "New Tip",
            Description = "New Description",
            Category = "Stress"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/tips", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(content, _jsonOptions);
        apiResponse!.Data!.Title.Should().Be("New Tip");
    }

    [Fact]
    public async Task Tips_Update_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar tip
        var createDto = new CreateTipDto
        {
            Title = "Original Title",
            Description = "Original Description",
            Category = "Wellness"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(createContent, _jsonOptions);
        var tipId = createApiResponse!.Data!.Id;

        // Act
        var updateDto = new UpdateTipDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Category = "Stress"
        };
        var response = await _client.PutAsJsonAsync($"/api/v1/tips/{tipId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(content, _jsonOptions);
        apiResponse!.Data!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task Tips_Delete_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar tip
        var createDto = new CreateTipDto
        {
            Title = "Tip to Delete",
            Description = "Description",
            Category = "Wellness"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(createContent, _jsonOptions);
        var tipId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/v1/tips/{tipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que foi deletado
        var getResponse = await _client.GetAsync($"/api/v1/tips/{tipId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Reports Endpoints

    [Fact]
    public async Task Reports_GetWeekly_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        // Act
        var response = await _client.GetAsync($"/api/v1/reports/weekly?weekStart={weekStart:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<WeeklyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Reports_GetMonthly_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=2024&month=11");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<MonthlyReportDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Year.Should().Be(2024);
        apiResponse.Data.Month.Should().Be(11);
    }

    #endregion

    #region Insights Endpoints

    [Fact]
    public async Task Insights_GetTrends_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/trends?period=week");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TrendsInsightDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Insights_GetStreak_Endpoint_Works()
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
        apiResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Insights_ComparePeriods_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
        apiResponse!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Insights_GetRecommendedTips_Endpoint_Works()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/insights/recommended-tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<TipDto>>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"test_{Guid.NewGuid()}@test.com";
        var password = "Test123!";
        return await RegisterUser(email, password);
    }

    private async Task<string> RegisterUser(string email, string password)
    {
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Name = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        
        return apiResponse?.Data?.Token ?? throw new Exception("Failed to get token");
    }

    #endregion
}

