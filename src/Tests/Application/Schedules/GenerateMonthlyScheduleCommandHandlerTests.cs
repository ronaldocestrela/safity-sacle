using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.Schedules;

public class GenerateMonthlyScheduleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAlreadyExists_WhenScheduleExistsForMonthYear()
    {
        var existing = new MonthlySchedule { Id = Guid.NewGuid(), Month = 8, Year = 2033 };
        var scheduleRepo = new InMemoryMonthlyScheduleRepository(existing);
        var guardRepo = new InMemorySecurityGuardRepository(
            new SecurityGuard { Id = Guid.NewGuid(), Name = "A", IsActive = true });
        var sectorRepo = new StubSectorRepository();
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new GenerateMonthlyScheduleCommandHandler(
            scheduleRepo,
            guardRepo,
            sectorRepo,
            unavailableRepo,
            unitOfWork,
            NullLogger<GenerateMonthlyScheduleCommandHandler>.Instance);

        var result = await handler.Handle(new GenerateMonthlyScheduleCommand(8, 2033), CancellationToken.None);

        result.Status.Should().Be(GenerateMonthlyScheduleStatus.AlreadyExists);
        scheduleRepo.Items.Should().HaveCount(1);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnNoActiveGuards_WhenNoneActive()
    {
        var scheduleRepo = new InMemoryMonthlyScheduleRepository();
        var guardRepo = new InMemorySecurityGuardRepository(
            new SecurityGuard { Id = Guid.NewGuid(), Name = "X", IsActive = false });
        var sectorRepo = new StubSectorRepository();
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new GenerateMonthlyScheduleCommandHandler(
            scheduleRepo,
            guardRepo,
            sectorRepo,
            unavailableRepo,
            unitOfWork,
            NullLogger<GenerateMonthlyScheduleCommandHandler>.Instance);

        var result = await handler.Handle(new GenerateMonthlyScheduleCommand(9, 2033), CancellationToken.None);

        result.Status.Should().Be(GenerateMonthlyScheduleStatus.NoActiveGuards);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnNoWorkload_WhenNoSectorsWithRequirementsConfigured()
    {
        var scheduleRepo = new InMemoryMonthlyScheduleRepository();
        var guardRepo = new InMemorySecurityGuardRepository(
            new SecurityGuard { Id = Guid.NewGuid(), Name = "A", IsActive = true });
        var sectorRepo = new StubSectorRepository(); // workload empty
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new GenerateMonthlyScheduleCommandHandler(
            scheduleRepo,
            guardRepo,
            sectorRepo,
            unavailableRepo,
            unitOfWork,
            NullLogger<GenerateMonthlyScheduleCommandHandler>.Instance);

        var result = await handler.Handle(new GenerateMonthlyScheduleCommand(1, 2035), CancellationToken.None);

        result.Status.Should().Be(GenerateMonthlyScheduleStatus.NoWorkloadSectorsConfigured);
        scheduleRepo.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPersist_WhenGenerationSucceeds()
    {
        var scheduleRepo = new InMemoryMonthlyScheduleRepository();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var ga = new SecurityGuard { Id = g1, Name = "Alpha", IsActive = true };
        var gb = new SecurityGuard { Id = g2, Name = "Bravo", IsActive = true };
        var guardRepo = new InMemorySecurityGuardRepository(ga, gb);
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();

        var sid = Guid.NewGuid();
        var sector = new Sector
        {
            Id = sid,
            Name = "Primary",
            RequiredGuardsPerDay = 1,
            IsActive = true,
            SecurityGuardSectors = new List<SecurityGuardSector>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SecurityGuardId = g1,
                    SectorId = sid,
                    SecurityGuard = ga,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    SecurityGuardId = g2,
                    SectorId = sid,
                    SecurityGuard = gb,
                },
            },
        };
        var sectorRepo = new StubSectorRepository(sector);

        var handler = new GenerateMonthlyScheduleCommandHandler(
            scheduleRepo,
            guardRepo,
            sectorRepo,
            unavailableRepo,
            unitOfWork,
            NullLogger<GenerateMonthlyScheduleCommandHandler>.Instance);

        var result = await handler.Handle(new GenerateMonthlyScheduleCommand(11, 2033), CancellationToken.None);

        result.Status.Should().Be(GenerateMonthlyScheduleStatus.Success);
        result.ScheduleId.Should().NotBeNull();
        scheduleRepo.Items.Should().ContainSingle(s => s.Id == result.ScheduleId);
        scheduleRepo.Items.Single().Items.Should().HaveCount(DateTime.DaysInMonth(2033, 11));
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnImpossible_WhenNoCoveragePossible()
    {
        var scheduleRepo = new InMemoryMonthlyScheduleRepository();
        var guardId = Guid.NewGuid();
        var guard = new SecurityGuard { Id = guardId, Name = "Only", IsActive = true };

        var sectorId = Guid.NewGuid();
        var sector = new Sector
        {
            Id = sectorId,
            Name = "Primary",
            RequiredGuardsPerDay = 1,
            IsActive = true,
            SecurityGuardSectors = new List<SecurityGuardSector>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SecurityGuardId = guardId,
                    SectorId = sectorId,
                    SecurityGuard = guard,
                },
            },
        };
        var sectorRepo = new StubSectorRepository(sector);

        var guardRepo = new InMemorySecurityGuardRepository(guard);

        var unavailable = new List<UnavailableDay>();
        var start = new DateOnly(2034, 6, 1);
        var end = new DateOnly(2034, 6, DateTime.DaysInMonth(2034, 6));
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            unavailable.Add(new UnavailableDay
            {
                Id = Guid.NewGuid(),
                SecurityGuardId = guardId,
                Date = d,
            });
        }

        var unavailableRepo = new InMemoryUnavailableDayRepository(unavailable.ToArray());
        var unitOfWork = new FakeUnitOfWork();

        var handler = new GenerateMonthlyScheduleCommandHandler(
            scheduleRepo,
            guardRepo,
            sectorRepo,
            unavailableRepo,
            unitOfWork,
            NullLogger<GenerateMonthlyScheduleCommandHandler>.Instance);

        var result = await handler.Handle(new GenerateMonthlyScheduleCommand(6, 2034), CancellationToken.None);

        result.Status.Should().Be(GenerateMonthlyScheduleStatus.ImpossibleToGenerate);
        result.FailedDate.Should().NotBeNull();
        scheduleRepo.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private sealed class StubSectorRepository : ISectorRepository
    {
        private readonly IReadOnlyList<Sector> _workload;

        public StubSectorRepository(params Sector[] workloadConfigured)
            => _workload = workloadConfigured;

        public Task AddAsync(Sector sector, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_workload.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)_workload.ToList());

        public void Update(Sector sector)
        {
        }

        public Task<bool> AllExistAndActiveAsync(IReadOnlyList<Guid> sectorIds, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<Guid?> GetDefaultSchedulingSectorIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Guid?>(null);

        public Task<IReadOnlyList<Sector>> GetActiveWorkloadSectorsWithLinksAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)_workload
                .Where(s => s.IsActive && s.RequiredGuardsPerDay >= 1)
                .OrderBy(s => s.Name)
                .ToList());
    }

    private sealed class InMemoryMonthlyScheduleRepository(params MonthlySchedule[] initial) : IMonthlyScheduleRepository
    {
        public List<MonthlySchedule> Items { get; } = initial.ToList();

        public Task AddAsync(MonthlySchedule monthlySchedule, CancellationToken cancellationToken = default)
        {
            Items.Add(monthlySchedule);
            return Task.CompletedTask;
        }

        public Task<MonthlySchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<MonthlySchedule?> GetByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Month == month && x.Year == year));

        public Task<bool> ExistsByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(x => x.Month == month && x.Year == year));
    }

    private sealed class InMemorySecurityGuardRepository(params SecurityGuard[] guards) : ISecurityGuardRepository
    {
        private readonly List<SecurityGuard> _items = guards.ToList();

        public Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default)
        {
            _items.Add(securityGuard);
            return Task.CompletedTask;
        }

        public Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)_items.ToList());

        public Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)_items.Where(x => x.IsActive).OrderBy(x => x.Name).ToList());

        public void Update(SecurityGuard securityGuard)
        {
            var i = _items.FindIndex(x => x.Id == securityGuard.Id);
            if (i >= 0)
            {
                _items[i] = securityGuard;
            }
        }
    }

    private sealed class InMemoryUnavailableDayRepository(params UnavailableDay[] initial) : IUnavailableDayRepository
    {
        public List<UnavailableDay> Items { get; } = initial.ToList();

        public Task AddAsync(UnavailableDay unavailableDay, CancellationToken cancellationToken = default)
        {
            Items.Add(unavailableDay);
            return Task.CompletedTask;
        }

        public Task<UnavailableDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<UnavailableDay>> GetByGuardIdAsync(Guid securityGuardId, CancellationToken cancellationToken = default)
        {
            var list = Items.Where(x => x.SecurityGuardId == securityGuardId).OrderBy(x => x.Date).ToList();
            return Task.FromResult((IReadOnlyList<UnavailableDay>)list);
        }

        public Task<IReadOnlyList<UnavailableDay>> GetByDateRangeAsync(
            DateOnly startInclusive,
            DateOnly endInclusive,
            CancellationToken cancellationToken = default)
        {
            var list = Items
                .Where(x => x.Date >= startInclusive && x.Date <= endInclusive)
                .OrderBy(x => x.Date)
                .ToList();
            return Task.FromResult((IReadOnlyList<UnavailableDay>)list);
        }

        public Task<bool> ExistsForGuardAndDateAsync(Guid securityGuardId, DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(x => x.SecurityGuardId == securityGuardId && x.Date == date));

        public void Remove(UnavailableDay unavailableDay)
        {
            var i = Items.FindIndex(x => x.Id == unavailableDay.Id);
            if (i >= 0)
            {
                Items.RemoveAt(i);
            }
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
