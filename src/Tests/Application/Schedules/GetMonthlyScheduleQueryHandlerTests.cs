using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.Schedules;

public class GetMonthlyScheduleQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenScheduleMissing()
    {
        var repo = new InMemoryMonthlyScheduleRepository();
        var handler = new GetMonthlyScheduleQueryHandler(repo);

        var result = await handler.Handle(new GetMonthlyScheduleQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnProjection_WhenScheduleExists()
    {
        var guardId = Guid.NewGuid();
        var guard = new SecurityGuard
        {
            Id = guardId,
            Name = "Guard A",
            IsActive = true,
        };
        var sectorId = Guid.NewGuid();
        var sector = new Sector { Id = sectorId, Name = "North Gate", RequiredGuardsPerDay = 1, IsActive = true };
        var scheduleId = Guid.NewGuid();
        var item1 = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            MonthlyScheduleId = scheduleId,
            SecurityGuardId = guardId,
            SectorId = sectorId,
            Date = new DateOnly(2042, 7, 15),
            IsWeekend = false,
            SecurityGuard = guard,
            Sector = sector,
        };
        var item2 = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            MonthlyScheduleId = scheduleId,
            SecurityGuardId = guardId,
            SectorId = sectorId,
            Date = new DateOnly(2042, 7, 2),
            IsWeekend = false,
            SecurityGuard = guard,
            Sector = sector,
        };
        var schedule = new MonthlySchedule
        {
            Id = scheduleId,
            Month = 7,
            Year = 2042,
            GeneratedAt = new DateTime(2042, 7, 1, 12, 0, 0, DateTimeKind.Utc),
            Items = new List<ScheduleItem> { item1, item2 },
        };
        var repo = new InMemoryMonthlyScheduleRepository(schedule);
        var handler = new GetMonthlyScheduleQueryHandler(repo);

        var result = await handler.Handle(new GetMonthlyScheduleQuery(scheduleId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(scheduleId);
        result.Month.Should().Be(7);
        result.Year.Should().Be(2042);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Date).Should().ContainInOrder(new DateOnly(2042, 7, 2), new DateOnly(2042, 7, 15));
        result.Items.Should().AllSatisfy(x =>
        {
            x.SecurityGuardId.Should().Be(guardId);
            x.SecurityGuardName.Should().Be("Guard A");
            x.SecurityGuardIsActive.Should().BeTrue();
            x.SectorId.Should().Be(sectorId);
            x.SectorName.Should().Be("North Gate");
        });
    }

    private sealed class InMemoryMonthlyScheduleRepository(params MonthlySchedule[] initial) : IMonthlyScheduleRepository
    {
        private readonly List<MonthlySchedule> _items = initial.ToList();

        public Task AddAsync(MonthlySchedule monthlySchedule, CancellationToken cancellationToken = default)
        {
            _items.Add(monthlySchedule);
            return Task.CompletedTask;
        }

        public Task<MonthlySchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<MonthlySchedule?> GetByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Month == month && x.Year == year));

        public Task<bool> ExistsByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(x => x.Month == month && x.Year == year));
    }
}
