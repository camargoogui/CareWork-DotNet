using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareWork.API.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareWork.Tests.IntegrationTests;

public class PaginationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaginationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetCheckins_Pagination_FirstPage_HasNextPage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar mais itens que o pageSize
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
        pagedResponse!.HasNextPage.Should().BeTrue();
        pagedResponse.HasPreviousPage.Should().BeFalse();
        pagedResponse.Page.Should().Be(1);
        pagedResponse.PageSize.Should().Be(10);
        pagedResponse.TotalPages.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetCheckins_Pagination_MiddlePage_HasBothLinks()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar itens
        for (int i = 0; i < 25; i++)
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
        var response = await _client.GetAsync("/api/v1/checkins?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.HasNextPage.Should().BeTrue();
        pagedResponse.HasPreviousPage.Should().BeTrue();
        pagedResponse.Links!.Previous.Should().NotBeNull();
        pagedResponse.Links.Next.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckins_Pagination_LastPage_HasPreviousPage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar itens
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
        var response = await _client.GetAsync("/api/v1/checkins?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.HasNextPage.Should().BeFalse();
        pagedResponse.HasPreviousPage.Should().BeTrue();
        pagedResponse.Links!.Next.Should().BeNull();
        pagedResponse.Links.Previous.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckins_Pagination_HATEOAS_LinksAreCorrect()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar itens
        for (int i = 0; i < 25; i++)
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
        var response = await _client.GetAsync("/api/v1/checkins?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Links.Should().NotBeNull();
        pagedResponse.Links!.Self.Should().Contain("page=2");
        pagedResponse.Links.Self.Should().Contain("pageSize=10");
        pagedResponse.Links.First.Should().Contain("page=1");
        pagedResponse.Links.Last.Should().Contain($"page={pagedResponse.TotalPages}");
    }

    [Fact]
    public async Task GetTips_Pagination_WorksCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar tips
        for (int i = 0; i < 12; i++)
        {
            var createDto = new CreateTipDto
            {
                Title = $"Tip {i}",
                Description = $"Description {i}",
                Category = "Wellness"
            };
            await _client.PostAsJsonAsync("/api/v1/tips", createDto);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/tips?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<TipDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Page.Should().Be(1);
        pagedResponse.PageSize.Should().Be(5);
        pagedResponse.Data.Count.Should().BeLessThanOrEqualTo(5);
        pagedResponse.Links.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckins_Pagination_InvalidPage_DefaultsToPage1()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetCheckins_Pagination_InvalidPageSize_DefaultsTo10()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/checkins?page=1&pageSize=200"); // Máximo é 100

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var pagedResponse = JsonSerializer.Deserialize<PagedResponseDto<CheckinDto>>(content, _jsonOptions);
        pagedResponse.Should().NotBeNull();
        pagedResponse!.PageSize.Should().Be(10); // Deve ser ajustado para 10
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

