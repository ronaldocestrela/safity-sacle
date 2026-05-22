using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.SecurityGuards.Commands.SetSecurityGuardSectors;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.SecurityGuards;

public class SetSecurityGuardSectorsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnGuardNotFound_WhenMissing()
    {
        var handler = new SetSecurityGuardSectorsCommandHandler(
            new InMemorySecurityGuardRepository(),
            new RecordingSecurityGuardSectorRepository(),
            new InMemorySectorRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new SetSecurityGuardSectorsCommand(Guid.NewGuid(), [Guid.NewGuid()]),
            CancellationToken.None);

        result.Should().Be(SetSecurityGuardSectorsStatus.GuardNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidSectors_WhenSectorMissing()
    {
        var guardId = Guid.NewGuid();
        var guard = new SecurityGuard { Id = guardId, Name = "G", IsActive = true };
        var handler = new SetSecurityGuardSectorsCommandHandler(
            new InMemorySecurityGuardRepository(guard),
            new RecordingSecurityGuardSectorRepository(),
            new InMemorySectorRepository(),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new SetSecurityGuardSectorsCommand(guardId, [Guid.NewGuid()]),
            CancellationToken.None);

        result.Should().Be(SetSecurityGuardSectorsStatus.InvalidSectors);
    }

    [Fact]
    public async Task Handle_ShouldReplaceLinks_WhenValid()
    {
        var guardId = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var guard = new SecurityGuard { Id = guardId, Name = "G", IsActive = true };
        var repo = new InMemorySectorRepository(
            new Sector { Id = s1, Name = "A", IsActive = true },
            new Sector { Id = s2, Name = "B", IsActive = true });
        var links = new RecordingSecurityGuardSectorRepository();
        var uow = new FakeUnitOfWork();
        var handler = new SetSecurityGuardSectorsCommandHandler(
            new InMemorySecurityGuardRepository(guard),
            links,
            repo,
            uow);

        var result = await handler.Handle(
            new SetSecurityGuardSectorsCommand(guardId, [s2, s1]),
            CancellationToken.None);

        result.Should().Be(SetSecurityGuardSectorsStatus.Success);
        uow.SaveChangesCalls.Should().Be(1);
        links.LastGuardId.Should().Be(guardId);
        links.LastSectorIds.Should().BeEquivalentTo([s2, s1]);
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

    private sealed class InMemorySecurityGuardRepository(params SecurityGuard[] items) : ISecurityGuardRepository
    {
        private readonly List<SecurityGuard> _items = items.ToList();

        public Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)_items.ToList());

        public Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)_items.Where(x => x.IsActive).OrderBy(x => x.Name).ToList());

        public void Update(SecurityGuard securityGuard)
        {
        }
    }

    private sealed class InMemorySectorRepository(params Sector[] items) : ISectorRepository
    {
        private readonly List<Sector> _items = items.ToList();

        public Task AddAsync(Sector sector, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)_items.ToList());

        public void Update(Sector sector)
        {
        }

        public Task<bool> AllExistAndActiveAsync(IReadOnlyList<Guid> sectorIds, CancellationToken cancellationToken = default)
        {
            var distinct = sectorIds.Distinct().ToList();
            if (distinct.Count == 0)
            {
                return Task.FromResult(true);
            }

            var ok = distinct.All(id => _items.Any(s => s.Id == id && s.IsActive));
            return Task.FromResult(ok);
        }
    }

    private sealed class RecordingSecurityGuardSectorRepository : ISecurityGuardSectorRepository
    {
        public Guid? LastGuardId { get; private set; }
        public IReadOnlyList<Guid>? LastSectorIds { get; private set; }

        public Task ReplaceAssignmentsForGuardAsync(
            Guid securityGuardId,
            IReadOnlyList<Guid> sectorIds,
            CancellationToken cancellationToken = default)
        {
            LastGuardId = securityGuardId;
            LastSectorIds = sectorIds;
            return Task.CompletedTask;
        }
    }
}
