using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = $"test_{Guid.NewGuid()}@test.com",
            Password = "Test123!",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "123",
            Name = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@test.com";
        var password = "Test123!";

        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Name = "Test User"
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<AuthResponseDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Email = $"updated_{Guid.NewGuid()}@test.com"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<UserDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdateProfileDto
        {
            Name = "string", // Inválido
            Email = "invalid-email" // Inválido
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email1 = $"user1_{Guid.NewGuid()}@test.com";
        var email2 = $"user2_{Guid.NewGuid()}@test.com";

        // Criar primeiro usuário
        var token1 = await RegisterAndGetToken(email1, "Test123!");
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);

        // Criar segundo usuário
        var token2 = await RegisterAndGetToken(email2, "Test123!");

        // Tentar atualizar perfil do segundo usuário com email do primeiro
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        var updateDto = new UpdateProfileDto
        {
            Name = "Test User",
            Email = email1 // Email já em uso
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var password = "Test123!";
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = password,
            NewPassword = "NewPassword123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/password", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<object>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePassword_WithWrongCurrentPassword_ReturnsUnauthorized()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/password", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePassword_WithSamePassword_ReturnsBadRequest()
    {
        // Arrange
        var password = "Test123!";
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = password,
            NewPassword = password // Mesma senha
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/auth/password", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteAccount_WithValidPassword_ReturnsOk()
    {
        // Arrange
        var password = "Test123!";
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var deleteDto = new DeleteAccountDto
        {
            Password = password
        };

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAccount_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var deleteDto = new DeleteAccountDto
        {
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        var deleteDto = new DeleteAccountDto
        {
            Password = "Test123!"
        };

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth/account")
        {
            Content = JsonContent.Create(deleteDto)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        return await RegisterAndGetToken($"test_{Guid.NewGuid()}@test.com", "Test123!");
    }

    private async Task<string> RegisterAndGetToken(string email, string password)
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
}

