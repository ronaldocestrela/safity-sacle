using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.SecurityGuards.Queries.GetSecurityGuards;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.SecurityGuards;

public class GetSecurityGuardsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAll_WhenFilterIsNull()
    {
        var repository = new InMemorySecurityGuardRepository(
            new SecurityGuard { Id = Guid.NewGuid(), Name = "A", IsActive = true },
            new SecurityGuard { Id = Guid.NewGuid(), Name = "B", IsActive = false });
        var handler = new GetSecurityGuardsQueryHandler(repository);

        var result = await handler.Handle(new GetSecurityGuardsQuery(null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsActive_WhenFilterIsProvided()
    {
        var repository = new InMemorySecurityGuardRepository(
            new SecurityGuard { Id = Guid.NewGuid(), Name = "A", IsActive = true },
            new SecurityGuard { Id = Guid.NewGuid(), Name = "B", IsActive = false });
        var handler = new GetSecurityGuardsQueryHandler(repository);

        var result = await handler.Handle(new GetSecurityGuardsQuery(true), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().IsActive.Should().BeTrue();
    }

    private sealed class InMemorySecurityGuardRepository(params SecurityGuard[] initialItems) : ISecurityGuardRepository
    {
        private readonly List<SecurityGuard> _items = initialItems.ToList();

        public Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SecurityGuard>)_items.ToList());

        public void Update(SecurityGuard securityGuard)
        {
        }
    }
}
