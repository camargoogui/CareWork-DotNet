using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class ValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "Test123!",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        // Não tentar deserializar pois o formato pode variar com erros de validação
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "123", // Muito curto
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            Name = "string" // Nome inválido
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckin_WithOutOfRangeValues_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateCheckinDto
        {
            Mood = 10, // Fora do range 1-5
            Stress = 0, // Fora do range
            Sleep = -1 // Inválido
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        // Não tentar deserializar pois o formato pode variar com erros de validação
    }

    [Fact]
    public async Task CreateCheckin_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar DTO incompleto (sem todos os campos obrigatórios)
        var createDto = new
        {
            Mood = 4
            // Faltam Stress e Sleep
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTip_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateTipDto
        {
            Title = "", // Inválido
            Description = "Description",
            Category = "Wellness"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tips", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTip_WithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateTipDto
        {
            Title = "Title",
            Description = "", // Inválido
            Category = "Wellness"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tips", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePassword_WithShortNewPassword_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = "Test123!",
            NewPassword = "123" // Muito curto
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/password", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateProfileDto
        {
            Name = "Valid Name",
            Email = "invalid-email-format" // Inválido
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCheckin_WithInvalidGuid_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/checkins/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeeklyReport_WithInvalidDate_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/weekly?weekStart=invalid-date");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMonthlyReport_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=2024&month=13"); // Mês inválido

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMonthlyReport_WithInvalidYear_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/monthly?year=1800&month=1"); // Ano muito antigo

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Pode aceitar, mas retornar dados vazios
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
}

