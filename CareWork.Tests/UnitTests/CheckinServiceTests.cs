using Microsoft.EntityFrameworkCore;
using AutoMapper;
using CareWork.API.Models.DTOs;
using CareWork.API.Services;
using CareWork.Infrastructure.Data;
using CareWork.Infrastructure.Models;
using Xunit;
using FluentAssertions;
using Moq;

namespace CareWork.Tests.UnitTests;

public class CheckinServiceTests : IDisposable
{
    private readonly CareWorkDbContext _context;
    private readonly CheckinService _service;
    private readonly Guid _testUserId;
    private readonly Mock<IMapper> _mapperMock;

    public CheckinServiceTests()
    {
        var options = new DbContextOptionsBuilder<CareWorkDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CareWorkDbContext(options);
        
        // Configurar AutoMapper mock
        _mapperMock = new Mock<IMapper>();
        _mapperMock.Setup(m => m.Map<CheckinDto>(It.IsAny<Checkin>()))
            .Returns<Checkin>(c => new CheckinDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Mood = c.Mood,
                Stress = c.Stress,
                Sleep = c.Sleep,
                Notes = c.Notes,
                Tags = c.Tags ?? new List<string>(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

        _service = new CheckinService(_context, _mapperMock.Object);
        _testUserId = Guid.NewGuid();
    }

    [Fact]
    public async Task CreateCheckinAsync_WithValidData_ReturnsCheckinDto()
    {
        // Arrange
        var dto = new CreateCheckinDto
        {
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            Notes = "Test notes",
            Tags = new List<string> { "test", "work" }
        };

        // Act
        var result = await _service.CreateCheckinAsync(dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Mood.Should().Be(4);
        result.Stress.Should().Be(2);
        result.Sleep.Should().Be(5);
        result.Notes.Should().Be("Test notes");
        result.Tags.Should().Contain("test");
        result.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetCheckinsAsync_WithPagination_ReturnsPagedResponse()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 4,
                Stress = 2,
                Sleep = 5,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCheckinsAsync(_testUserId, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Data.Count.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.Links.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCheckinByIdAsync_WithValidId_ReturnsCheckinDto()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkin = new Checkin
        {
            Id = checkinId,
            UserId = _testUserId,
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            Notes = "Test notes",
            Tags = new List<string> { "test" },
            CreatedAt = DateTime.UtcNow
        };
        _context.Checkins.Add(checkin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCheckinByIdAsync(checkinId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(checkinId);
        result.Mood.Should().Be(4);
        result.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task GetCheckinByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetCheckinByIdAsync(Guid.NewGuid(), _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCheckinAsync_WithValidData_ReturnsUpdatedCheckinDto()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkin = new Checkin
        {
            Id = checkinId,
            UserId = _testUserId,
            Mood = 3,
            Stress = 3,
            Sleep = 3,
            CreatedAt = DateTime.UtcNow
        };
        _context.Checkins.Add(checkin);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateCheckinDto
        {
            Mood = 5,
            Stress = 1,
            Sleep = 5,
            Notes = "Updated notes",
            Tags = new List<string> { "updated" }
        };

        // Act
        var result = await _service.UpdateCheckinAsync(checkinId, updateDto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Mood.Should().Be(5);
        result.Stress.Should().Be(1);
        result.Sleep.Should().Be(5);
        result.Notes.Should().Be("Updated notes");
        result.Tags.Should().Contain("updated");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCheckinAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var checkinId = Guid.NewGuid();
        var checkin = new Checkin
        {
            Id = checkinId,
            UserId = _testUserId,
            Mood = 4,
            Stress = 2,
            Sleep = 5,
            CreatedAt = DateTime.UtcNow
        };
        _context.Checkins.Add(checkin);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteCheckinAsync(checkinId, _testUserId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Checkins.FindAsync(checkinId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetWeeklyReportAsync_WithCheckins_ReturnsReport()
    {
        // Arrange
        var weekStart = new DateTime(2024, 11, 4);
        for (int i = 0; i < 7; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 4,
                Stress = 2,
                Sleep = 5,
                CreatedAt = weekStart.AddDays(i)
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWeeklyReportAsync(_testUserId, weekStart);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.WeekStart.Should().Be(weekStart);
        result.Averages.Should().NotBeNull();
        result.DailyData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMonthlyReportAsync_WithCheckins_ReturnsReport()
    {
        // Arrange
        var year = 2024;
        var month = 11;
        for (int i = 1; i <= 10; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 4,
                Stress = 2,
                Sleep = 5,
                CreatedAt = new DateTime(year, month, i)
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMonthlyReportAsync(_testUserId, year, month);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.Year.Should().Be(year);
        result.Month.Should().Be(month);
        result.Averages.Should().NotBeNull();
        result.WeeklySummaries.Should().NotBeEmpty();
        result.TotalCheckins.Should().Be(10);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

