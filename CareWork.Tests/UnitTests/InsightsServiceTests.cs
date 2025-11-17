using Microsoft.EntityFrameworkCore;
using CareWork.API.Services;
using CareWork.Infrastructure.Data;
using CareWork.Infrastructure.Models;
using Xunit;
using FluentAssertions;

namespace CareWork.Tests.UnitTests;

public class InsightsServiceTests : IDisposable
{
    private readonly CareWorkDbContext _context;
    private readonly InsightsService _service;
    private readonly Guid _testUserId;

    public InsightsServiceTests()
    {
        var options = new DbContextOptionsBuilder<CareWorkDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CareWorkDbContext(options);
        
        // Mock ITipService
        var tipService = new Moq.Mock<ITipService>();
        _service = new InsightsService(_context, tipService.Object);
        _testUserId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetTrendsAsync_WithCheckins_ReturnsTrendsInsightDto()
    {
        // Arrange
        for (int i = 0; i < 7; i++)
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
        var result = await _service.GetTrendsAsync(_testUserId, "week");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.Period.Should().Be("week");
        result.Mood.Should().NotBeNull();
        result.Stress.Should().NotBeNull();
        result.Sleep.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStreakAsync_WithConsecutiveCheckins_ReturnsStreak()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        for (int i = 0; i < 5; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 4,
                Stress = 2,
                Sleep = 5,
                CreatedAt = today.AddDays(-i)
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStreakAsync(_testUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.CurrentStreak.Should().BeGreaterThan(0);
        result.LongestStreak.Should().BeGreaterThanOrEqualTo(result.CurrentStreak);
    }

    [Fact]
    public async Task GetStreakAsync_WithoutCheckins_ReturnsZero()
    {
        // Act
        var result = await _service.GetStreakAsync(_testUserId);

        // Assert
        result.Should().NotBeNull();
        result.CurrentStreak.Should().Be(0);
        result.LongestStreak.Should().Be(0);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ComparePeriodsAsync_WithCheckins_ReturnsComparison()
    {
        // Arrange
        var start1 = DateTime.UtcNow.AddDays(-14);
        var end1 = DateTime.UtcNow.AddDays(-7);
        var start2 = DateTime.UtcNow.AddDays(-7);
        var end2 = DateTime.UtcNow;

        // Check-ins no período 1
        for (int i = 0; i < 5; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 3,
                Stress = 3,
                Sleep = 3,
                CreatedAt = start1.AddDays(i)
            };
            _context.Checkins.Add(checkin);
        }

        // Check-ins no período 2
        for (int i = 0; i < 5; i++)
        {
            var checkin = new Checkin
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Mood = 5,
                Stress = 1,
                Sleep = 5,
                CreatedAt = start2.AddDays(i)
            };
            _context.Checkins.Add(checkin);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ComparePeriodsAsync(_testUserId, start1, end1, start2, end2);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.Period1.Should().NotBeNull();
        result.Period2.Should().NotBeNull();
        result.Comparison.Should().NotBeNull();
        result.Comparison.OverallTrend.Should().Be("better");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

