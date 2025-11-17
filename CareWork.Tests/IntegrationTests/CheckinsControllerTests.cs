using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class CheckinsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public CheckinsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetCheckins_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/checkins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCheckin_WithValidData_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCheckins_WithAuth_ReturnsPagedResponse()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar alguns check-ins
        for (int i = 0; i < 3; i++)
        {
            var createDto = new CreateCheckinDto
            {
                Mood = 4,
                Stress = 2,
                Sleep = 5
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Data.Should().NotBeNull();
        pagedResponse.Links.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckins_WithPagination_ReturnsCorrectLinks()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar mais check-ins para testar paginação
        for (int i = 0; i < 15; i++)
        {
            var createDto = new CreateCheckinDto
            {
                Mood = 4,
                Stress = 2,
                Sleep = 5
            };
            await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Page.Should().Be(1);
        pagedResponse.PageSize.Should().Be(10);
        pagedResponse.Links.Should().NotBeNull();
        pagedResponse.Links!.Self.Should().NotBeNull();
        pagedResponse.Links.First.Should().NotBeNull();
        pagedResponse.HasNextPage.Should().BeTrue();
        pagedResponse.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetCheckins_WithHATEOAS_ReturnsAllLinks()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=2&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Links.Should().NotBeNull();
        pagedResponse.Links!.Self.Should().Contain("page=2");
        pagedResponse.Links.First.Should().NotBeNull();
        pagedResponse.Links.Last.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckin_ById_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar um check-in
        var createDto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            Notes = "Test notes",
            Tags = new List<string> { "test", "work" }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(createContent, _jsonOptions);
        var checkinId = createApiResponse!.Data!.Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/checkins/{checkinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(checkinId);
        apiResponse.Data.Notes.Should().Be("Test notes");
        apiResponse.Data.Tags.Should().Contain("test");
    }

    [Fact]
    public async Task UpdateCheckin_WithValidData_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar um check-in
        var createDto = new CreateCheckinDto
        {
            Mood = 3,
            Stress = 3,
            Sleep = 3
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createApiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(createContent, _jsonOptions);
        var checkinId = createApiResponse!.Data!.Id;

        var updateDto = new UpdateCheckinDto
        {
            Mood = 5,
            Stress = 1,
            Sleep = 5,
            Notes = "Updated notes",
            Tags = new List<string> { "updated" }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/checkins/{checkinId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Mood.Should().Be(5);
        apiResponse.Data.Stress.Should().Be(1);
        apiResponse.Data.Sleep.Should().Be(5);
        apiResponse.Data.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task DeleteCheckin_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar um check-in
        var createDto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);
        createResponse.EnsureSuccessStatusCode();
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

    [Fact]
    public async Task CreateCheckin_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var invalidDto = new CreateCheckinDto
        {
            Mood = 10, // Inválido (deve ser 1-5)
            Stress = 0, // Inválido
            Sleep = 6 // Inválido
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", invalidDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckin_WithNotesAndTags_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            Notes = "Had a great day at work!",
            Tags = new List<string> { "work", "productive", "happy" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/checkins", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<CheckinDto>>(content, _jsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Notes.Should().Be("Had a great day at work!");
        apiResponse.Data.Tags.Should().Contain("work");
        apiResponse.Data.Tags.Should().Contain("productive");
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

