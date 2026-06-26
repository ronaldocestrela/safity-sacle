using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;
using SafetyScale.Domain.Entities;
using SafetyScale.Tests.Application.Common;

namespace SafetyScale.Tests.Application.Schedules;

public class GetMonthlySchedulesQueryHandlerTests
{
    private static readonly FakeCurrentUserContext UnrestrictedUser = FakeCurrentUserContext.Unrestricted;
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenScheduleMissing()
    {
        var repo = new InMemoryMonthlyScheduleRepository();
        var handler = new GetMonthlySchedulesQueryHandler(repo, UnrestrictedUser);

        var result = await handler.Handle(new GetMonthlySchedulesQuery(3, 2043), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnProjection_WhenMonthYearMatches()
    {
        var guardId = Guid.NewGuid();
        var guard = new SecurityGuard { Id = guardId, Name = "Beta", IsActive = false };
        var sectorId = Guid.NewGuid();
        var sector = new Sector { Id = sectorId, Name = "Lobby", RequiredGuardsPerDay = 1, IsActive = true };
        var scheduleId = Guid.NewGuid();
        var schedule = new MonthlySchedule
        {
            Id = scheduleId,
            Month = 10,
            Year = 2051,
            GeneratedAt = new DateTime(2051, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            Items = new List<ScheduleItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    MonthlyScheduleId = scheduleId,
                    SecurityGuardId = guardId,
                    SectorId = sectorId,
                    Date = new DateOnly(2051, 10, 5),
                    IsWeekend = false,
                    SecurityGuard = guard,
                    Sector = sector,
                },
            },
        };
        var repo = new InMemoryMonthlyScheduleRepository(schedule);
        var handler = new GetMonthlySchedulesQueryHandler(repo, UnrestrictedUser);

        var result = await handler.Handle(new GetMonthlySchedulesQuery(10, 2051), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Month.Should().Be(10);
        result.Year.Should().Be(2051);
        result.Items.Should().ContainSingle(i =>
            i.SecurityGuardName == "Beta" &&
            !i.SecurityGuardIsActive &&
            i.SecurityGuardId == guardId &&
            i.SectorId == sectorId &&
            i.SectorName == "Lobby");
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
