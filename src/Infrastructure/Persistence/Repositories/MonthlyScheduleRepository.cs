using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public class MonthlyScheduleRepository(ApplicationDbContext dbContext) : IMonthlyScheduleRepository
{
    public async Task AddAsync(MonthlySchedule monthlySchedule, CancellationToken cancellationToken = default)
        => await dbContext.MonthlySchedules.AddAsync(monthlySchedule, cancellationToken);

    public async Task<MonthlySchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.MonthlySchedules
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<MonthlySchedule?> GetByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default)
        => await dbContext.MonthlySchedules
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Month == month && x.Year == year, cancellationToken);
}
