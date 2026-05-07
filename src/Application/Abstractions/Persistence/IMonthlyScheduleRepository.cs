using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Persistence;

public interface IMonthlyScheduleRepository
{
    Task AddAsync(MonthlySchedule monthlySchedule, CancellationToken cancellationToken = default);
    Task<MonthlySchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MonthlySchedule?> GetByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<bool> ExistsByMonthYearAsync(int month, int year, CancellationToken cancellationToken = default);
}
