using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.UnavailableDays.Commands.AddUnavailableDay;
using SafetyScale.Application.UnavailableDays.Commands.RemoveUnavailableDay;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.UnavailableDays;

public class UnavailableDayCommandHandlersTests
{
    [Fact]
    public async Task AddCommand_ShouldPersist_WhenGuardIsActiveAndDayIsFree()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = true };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new AddUnavailableDayCommandHandler(guardRepo, unavailableRepo, unitOfWork);

        var date = new DateOnly(2030, 6, 15);
        var result = await handler.Handle(
            new AddUnavailableDayCommand(guard.Id, date, "  Folga médica "),
            CancellationToken.None);

        result.Status.Should().Be(AddUnavailableDayStatus.Success);
        result.Id.Should().NotBeNull();
        unavailableRepo.Items.Should().ContainSingle();
        unavailableRepo.Items[0].Date.Should().Be(date);
        unavailableRepo.Items[0].Reason.Should().Be("Folga médica");
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AddCommand_ShouldNormalizeEmptyReasonToNull()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = true };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddUnavailableDayCommandHandler(guardRepo, unavailableRepo, unitOfWork);

        var result = await handler.Handle(
            new AddUnavailableDayCommand(guard.Id, new DateOnly(2030, 7, 1), "   "),
            CancellationToken.None);

        result.Status.Should().Be(AddUnavailableDayStatus.Success);
        unavailableRepo.Items.Single().Reason.Should().BeNull();
    }

    [Fact]
    public async Task AddCommand_ShouldReturnGuardNotFound_WhenGuardMissing()
    {
        var guardRepo = new InMemorySecurityGuardRepository();
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddUnavailableDayCommandHandler(guardRepo, unavailableRepo, unitOfWork);

        var result = await handler.Handle(
            new AddUnavailableDayCommand(Guid.NewGuid(), new DateOnly(2030, 8, 1), null),
            CancellationToken.None);

        result.Status.Should().Be(AddUnavailableDayStatus.GuardNotFound);
        unavailableRepo.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task AddCommand_ShouldReturnGuardInactive_WhenGuardIsInactive()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = false };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddUnavailableDayCommandHandler(guardRepo, unavailableRepo, unitOfWork);

        var result = await handler.Handle(
            new AddUnavailableDayCommand(guard.Id, new DateOnly(2030, 9, 1), null),
            CancellationToken.None);

        result.Status.Should().Be(AddUnavailableDayStatus.GuardInactive);
        unavailableRepo.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task AddCommand_ShouldReturnDuplicateDate_WhenSameDateExists()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = true };
        var date = new DateOnly(2030, 10, 1);

        var existing = new UnavailableDay
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = guard.Id,
            Date = date,
            Reason = "First",
        };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var unavailableRepo = new InMemoryUnavailableDayRepository(existing);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddUnavailableDayCommandHandler(guardRepo, unavailableRepo, unitOfWork);

        var result = await handler.Handle(
            new AddUnavailableDayCommand(guard.Id, date, "Second"),
            CancellationToken.None);

        result.Status.Should().Be(AddUnavailableDayStatus.DuplicateDate);
        unavailableRepo.Items.Should().HaveCount(1);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task RemoveCommand_ShouldReturnFalse_WhenUnavailableDayDoesNotExist()
    {
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveUnavailableDayCommandHandler(unavailableRepo, unitOfWork);

        var result = await handler.Handle(new RemoveUnavailableDayCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task RemoveCommand_ShouldRemove_WhenUnavailableDayExists()
    {
        var item = new UnavailableDay
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = Guid.NewGuid(),
            Date = new DateOnly(2030, 11, 1),
        };
        var unavailableRepo = new InMemoryUnavailableDayRepository(item);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RemoveUnavailableDayCommandHandler(unavailableRepo, unitOfWork);

        var result = await handler.Handle(new RemoveUnavailableDayCommand(item.Id), CancellationToken.None);

        result.Should().BeTrue();
        unavailableRepo.Items.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(1);
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

        public Task<IReadOnlyList<UnavailableDay>> GetByGuardIdAsync(
            Guid securityGuardId,
            CancellationToken cancellationToken = default)
        {
            var list = Items
                .Where(x => x.SecurityGuardId == securityGuardId)
                .OrderBy(x => x.Date)
                .ToList();
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

        public Task<bool> ExistsForGuardAndDateAsync(
            Guid securityGuardId,
            DateOnly date,
            CancellationToken cancellationToken = default)
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
