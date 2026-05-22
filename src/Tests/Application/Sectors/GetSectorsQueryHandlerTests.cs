using FluentAssertions;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Sectors.Queries.GetSectors;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Tests.Application.Sectors;

public class GetSectorsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAll_WhenFilterIsNull()
    {
        var repository = new InMemorySectorRepository(
            new Sector { Id = Guid.NewGuid(), Name = "A", IsActive = true },
            new Sector { Id = Guid.NewGuid(), Name = "B", IsActive = false });
        var handler = new GetSectorsQueryHandler(repository);

        var result = await handler.Handle(new GetSectorsQuery(null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsActive_WhenFilterIsProvided()
    {
        var repository = new InMemorySectorRepository(
            new Sector { Id = Guid.NewGuid(), Name = "A", IsActive = true },
            new Sector { Id = Guid.NewGuid(), Name = "B", IsActive = false });
        var handler = new GetSectorsQueryHandler(repository);

        var result = await handler.Handle(new GetSectorsQuery(true), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().IsActive.Should().BeTrue();
    }

    private sealed class InMemorySectorRepository(params Sector[] initialItems) : ISectorRepository
    {
        private readonly List<Sector> _items = initialItems.ToList();

        public Task AddAsync(Sector sector, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Sector>)_items.OrderBy(x => x.Name).ToList());

        public void Update(Sector sector)
        {
        }

        public Task<bool> AllExistAndActiveAsync(IReadOnlyList<Guid> sectorIds, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
