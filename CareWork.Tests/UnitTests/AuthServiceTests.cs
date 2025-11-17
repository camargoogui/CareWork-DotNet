using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;
using CareWork.Infrastructure.Data;
using CareWork.Infrastructure.Models;
using Xunit;
using FluentAssertions;

namespace CareWork.Tests.UnitTests;

public class AuthServiceTests : IDisposable
{
    private readonly CareWorkDbContext _context;
    private readonly AuthService _service;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<CareWorkDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CareWorkDbContext(options);

        // Configurar IConfiguration mock
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Key", "TestKeyWithAtLeast32CharactersForHS256Algorithm" },
            { "Jwt:Issuer", "CareWork" },
            { "Jwt:Audience", "CareWork" },
            { "Jwt:ExpirationMinutes", "1440" }
        });
        _configuration = configBuilder.Build();

        _service = new AuthService(_context, _configuration);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponseDto()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            Name = "Test User"
        };

        // Act
        var result = await _service.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            Name = "Test User"
        };

        await _service.RegisterAsync(dto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RegisterAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponseDto()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!";
        
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            Name = "Test User"
        };
        await _service.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ReturnsUserDto()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            Name = "Original Name"
        };
        var authResponse = await _service.RegisterAsync(registerDto);
        var userId = authResponse.UserId;

        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var password = "Test123!";
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            Name = "Test User"
        };
        var authResponse = await _service.RegisterAsync(registerDto);
        var userId = authResponse.UserId;

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = password,
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _service.UpdatePasswordAsync(userId, updateDto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            Name = "Test User"
        };
        var authResponse = await _service.RegisterAsync(registerDto);
        var userId = authResponse.UserId;

        var updateDto = new UpdatePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _service.UpdatePasswordAsync(userId, updateDto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccountAsync_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        var password = "Test123!";
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            Name = "Test User"
        };
        var authResponse = await _service.RegisterAsync(registerDto);
        var userId = authResponse.UserId;

        // Criar alguns check-ins
        for (int i = 0; i < 3; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Mood = 4,
                Stress = 2,
                Sleep = 5,
                CreatedAt = DateTime.UtcNow
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAccountAsync(userId, password);

        // Assert
        result.Should().BeTrue();
        var user = await _context.Users.FindAsync(userId);
        user.Should().BeNull();
        var checkins = await _context.Checkins.Where(c => c.UserId == userId).ToListAsync();
        checkins.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

