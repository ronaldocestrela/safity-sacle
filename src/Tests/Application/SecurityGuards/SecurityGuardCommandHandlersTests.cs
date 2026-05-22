using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.ActivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.SecurityGuards;

public class SecurityGuardCommandHandlersTests
{
    [Fact]
    public async Task CreateCommand_ShouldPersistActiveSecurityGuard()
    {
        var repository = new InMemorySecurityGuardRepository();
        var sectorRepository = new MinimalSectorRepository();
        var guardSectorRepository = new CreateGuardSectorLinkRecordingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateSecurityGuardCommandHandler(
            repository,
            sectorRepository,
            guardSectorRepository,
            unitOfWork);

        var id = await handler.Handle(new CreateSecurityGuardCommand("  Maria Silva  "), CancellationToken.None);

        repository.Items.Should().ContainSingle(x => x.Id == id);
        repository.Items.Single().Name.Should().Be("Maria Silva");
        repository.Items.Single().IsActive.Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(1);
        guardSectorRepository.EnsureCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCommand_ShouldLinkDefaultSector_WhenConfigured()
    {
        var defaultSectorId = Guid.NewGuid();
        var repository = new InMemorySecurityGuardRepository();
        var sectorRepository = new MinimalSectorRepository(defaultSectorId);
        var guardSectorRepository = new CreateGuardSectorLinkRecordingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateSecurityGuardCommandHandler(
            repository,
            sectorRepository,
            guardSectorRepository,
            unitOfWork);

        var id = await handler.Handle(new CreateSecurityGuardCommand("Link Test"), CancellationToken.None);

        unitOfWork.SaveChangesCalls.Should().Be(2);
        guardSectorRepository.EnsureCalls.Should().ContainSingle(c => c.GuardId == id && c.SectorId == defaultSectorId);
    }

    [Fact]
    public async Task UpdateCommand_ShouldReturnFalse_WhenSecurityGuardDoesNotExist()
    {
        var repository = new InMemorySecurityGuardRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new UpdateSecurityGuardCommand(Guid.NewGuid(), "Novo Nome"), CancellationToken.None);

        result.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCommand_ShouldUpdateName_WhenSecurityGuardExists()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Nome Antigo", IsActive = true };
        var repository = new InMemorySecurityGuardRepository(guard);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new UpdateSecurityGuardCommand(guard.Id, "  Nome Novo  "), CancellationToken.None);

        result.Should().BeTrue();
        repository.Items.Single().Name.Should().Be("Nome Novo");
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task InactivateCommand_ShouldSetInactive_WhenSecurityGuardIsActive()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Ativo", IsActive = true };
        var repository = new InMemorySecurityGuardRepository(guard);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new InactivateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new InactivateSecurityGuardCommand(guard.Id), CancellationToken.None);

        result.Should().BeTrue();
        repository.Items.Single().IsActive.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task InactivateCommand_ShouldBeIdempotent_WhenSecurityGuardIsAlreadyInactive()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Inativo", IsActive = false };
        var repository = new InMemorySecurityGuardRepository(guard);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new InactivateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new InactivateSecurityGuardCommand(guard.Id), CancellationToken.None);

        result.Should().BeTrue();
        repository.Items.Single().IsActive.Should().BeFalse();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task ActivateCommand_ShouldSetActive_WhenSecurityGuardIsInactive()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Inativo", IsActive = false };
        var repository = new InMemorySecurityGuardRepository(guard);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ActivateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new ActivateSecurityGuardCommand(guard.Id), CancellationToken.None);

        result.Should().BeTrue();
        repository.Items.Single().IsActive.Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task ActivateCommand_ShouldBeIdempotent_WhenSecurityGuardIsAlreadyActive()
    {
        var guard = new SecurityGuard { Id = Guid.NewGuid(), Name = "Ativo", IsActive = true };
        var repository = new InMemorySecurityGuardRepository(guard);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ActivateSecurityGuardCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new ActivateSecurityGuardCommand(guard.Id), CancellationToken.None);

        result.Should().BeTrue();
        repository.Items.Single().IsActive.Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private sealed class InMemorySecurityGuardRepository(params SecurityGuard[] initialItems) : ISecurityGuardRepository
    {
        public List<SecurityGuard> Items { get; } = initialItems.ToList();

        public Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default)
        {
            Items.Add(securityGuard);
            return Task.CompletedTask;
        }

        public Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)Items.ToList());

        public Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)Items.Where(x => x.IsActive).OrderBy(x => x.Name).ToList());

        public void Update(SecurityGuard securityGuard)
        {
            var index = Items.FindIndex(x => x.Id == securityGuard.Id);
            if (index >= 0)
            {
                Items[index] = securityGuard;
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

    private sealed class MinimalSectorRepository(Guid? defaultSchedulingSectorId = null) : ISectorRepository
    {
        public Task AddAsync(Sector sector, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Sector?>(null);

        public Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)Array.Empty<Sector>());

        public void Update(Sector sector)
        {
        }

        public Task<Guid?> GetDefaultSchedulingSectorIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(defaultSchedulingSectorId);

        public Task<IReadOnlyList<Sector>> GetActiveWorkloadSectorsWithLinksAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)Array.Empty<Sector>());

        public Task<bool> AllExistAndActiveAsync(IReadOnlyList<Guid> sectorIds, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class CreateGuardSectorLinkRecordingRepository : ISecurityGuardSectorRepository
    {
        public List<(Guid GuardId, Guid SectorId)> EnsureCalls { get; } = [];

        public Task ReplaceAssignmentsForGuardAsync(
            Guid securityGuardId,
            IReadOnlyList<Guid> sectorIds,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task EnsureGuardLinkedToSectorAsync(
            Guid securityGuardId,
            Guid sectorId,
            CancellationToken cancellationToken = default)
        {
            EnsureCalls.Add((securityGuardId, sectorId));
            return Task.CompletedTask;
        }
    }
}
