using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class TipsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public TipsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetTips_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tips");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTip_WithValidData_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateTipDto
        {
            Title = "Test Tip",
            Description = "This is a test tip description",
            Category = "Wellness"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tips", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTips_WithAuth_ReturnsPagedResponse()
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
        pagedResponse!.Data.Should().NotBeNull();
        pagedResponse.Links.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTip_ById_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar uma tip primeiro
        var createDto = new CreateTipDto
        {
            Title = "Test Tip",
            Description = "This is a test tip description",
            Category = "Wellness"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(createContent, _jsonOptions);
        var tipId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/tips/{tipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(tipId);
    }

    [Fact]
    public async Task GetTip_ById_NotFound_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/tips/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTip_WithValidData_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar uma tip primeiro
        var createDto = new CreateTipDto
        {
            Title = "Original Title",
            Description = "Original description",
            Category = "Wellness"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(createContent, _jsonOptions);
        var tipId = createApiResponse!.Data!.Id;

        var updateDto = new UpdateTipDto
        {
            Title = "Updated Title",
            Description = "Updated description",
            Category = "Stress"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/tips/{tipId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<TipDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Title.Should().Be("Updated Title");
        apiResponse.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteTip_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar uma tip primeiro
        var createDto = new CreateTipDto
        {
            Title = "Tip to Delete",
            Description = "This tip will be deleted",
            Category = "Wellness"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        createResponse.EnsureSuccessStatusCode();
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

    [Fact]
    public async Task GetTips_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar tips de diferentes categorias
        var stressTip = new CreateTipDto
        {
            Title = "Stress Tip",
            Description = "Stress management tip",
            Category = "Stress"
        };
        await _client.PostAsJsonAsync("/api/v1/tips", stressTip);

        var wellnessTip = new CreateTipDto
        {
            Title = "Wellness Tip",
            Description = "Wellness tip",
            Category = "Wellness"
        };
        await _client.PostAsJsonAsync("/api/v1/tips", wellnessTip);

        // Act
        var response = await _client.GetAsync("/api/v1/tips?category=Stress&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<TipDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Data.Should().NotBeNull();
        if (pagedResponse.Data.Any())
        {
            pagedResponse.Data.All(t => t.Category == "Stress").Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateTip_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var invalidDto = new CreateTipDto
        {
            Title = "", // Inválido
            Description = "", // Inválido
            Category = "Wellness"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/tips", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

