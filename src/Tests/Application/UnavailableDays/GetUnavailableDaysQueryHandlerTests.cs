using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.UnavailableDays.Queries.GetUnavailableDays;
using SafetyScale.Domain.Entities;
using SafetyScale.Tests.Application.Common;

namespace SafetyScale.Tests.Application.UnavailableDays;

public class GetUnavailableDaysQueryHandlerTests
{
    private static readonly FakeCurrentUserContext UnrestrictedUser = FakeCurrentUserContext.Unrestricted;
    [Fact]
    public async Task GetQuery_ShouldReturnGuardNotExists_WhenGuardMissing()
    {
        var guardRepo = new InMemorySecurityGuardRepository();
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var handler = new GetUnavailableDaysQueryHandler(guardRepo, unavailableRepo, UnrestrictedUser);

        var result = await handler.Handle(new GetUnavailableDaysQuery(Guid.NewGuid()), CancellationToken.None);

        result.GuardExists.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuery_ShouldReturnEmptyList_WhenGuardExistsButNoUnavailableDays()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = true };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var unavailableRepo = new InMemoryUnavailableDayRepository();
        var handler = new GetUnavailableDaysQueryHandler(guardRepo, unavailableRepo, UnrestrictedUser);

        var result = await handler.Handle(new GetUnavailableDaysQuery(guard.Id), CancellationToken.None);

        result.GuardExists.Should().BeTrue();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuery_ShouldReturnDaysOrderedByDate()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Guard", IsActive = true };
        var guardRepo = new InMemorySecurityGuardRepository(guard);
        var d1 = new UnavailableDay
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = guard.Id,
            Date = new DateOnly(2030, 3, 20),
            Reason = "B",
        };
        var d0 = new UnavailableDay
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = guard.Id,
            Date = new DateOnly(2030, 3, 5),
            Reason = "A",
        };
        var unavailableRepo = new InMemoryUnavailableDayRepository(d1, d0);
        var handler = new GetUnavailableDaysQueryHandler(guardRepo, unavailableRepo, UnrestrictedUser);

        var result = await handler.Handle(new GetUnavailableDaysQuery(guard.Id), CancellationToken.None);

        result.GuardExists.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Date).Should().ContainInOrder(new DateOnly(2030, 3, 5), new DateOnly(2030, 3, 20));
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

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count);

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
}
