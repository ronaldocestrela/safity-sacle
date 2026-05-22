using MediatR;
using Microsoft.Extensions.Logging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Services;

namespace SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;

public enum GenerateMonthlyScheduleStatus
{
    Success,
    AlreadyExists,
    NoActiveGuards,
    NoWorkloadSectorsConfigured,
    ImpossibleToGenerate,
}

public sealed record GenerateMonthlyScheduleResult(
    GenerateMonthlyScheduleStatus Status,
    Guid? ScheduleId = null,
    DateOnly? FailedDate = null);

public sealed record GenerateMonthlyScheduleCommand(int Month, int Year)
    : IRequest<GenerateMonthlyScheduleResult>;

public sealed class GenerateMonthlyScheduleCommandHandler(
    IMonthlyScheduleRepository monthlyScheduleRepository,
    ISecurityGuardRepository securityGuardRepository,
    ISectorRepository sectorRepository,
    IUnavailableDayRepository unavailableDayRepository,
    IUnitOfWork unitOfWork,
    ILogger<GenerateMonthlyScheduleCommandHandler> logger) : IRequestHandler<GenerateMonthlyScheduleCommand, GenerateMonthlyScheduleResult>
{
    public async Task<GenerateMonthlyScheduleResult> Handle(
        GenerateMonthlyScheduleCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Monthly schedule generation requested for {Month}/{Year}",
            request.Month,
            request.Year);

        if (await monthlyScheduleRepository.ExistsByMonthYearAsync(request.Month, request.Year, cancellationToken))
        {
            logger.LogWarning(
                "Monthly schedule already exists for {Month}/{Year}",
                request.Month,
                request.Year);
            return new GenerateMonthlyScheduleResult(GenerateMonthlyScheduleStatus.AlreadyExists);
        }

        var activeGuards = await securityGuardRepository.GetActiveAsync(cancellationToken);
        if (activeGuards.Count == 0)
        {
            logger.LogWarning("No active security guards available for schedule generation");
            return new GenerateMonthlyScheduleResult(GenerateMonthlyScheduleStatus.NoActiveGuards);
        }

        var workloadSectors = await sectorRepository.GetActiveWorkloadSectorsWithLinksAsync(cancellationToken);
        var activeGuardIds = activeGuards.Select(g => g.Id).ToHashSet();

        var sectorDefinitions = new List<SectorWorkloadDefinition>();
        foreach (var sector in workloadSectors)
        {
            var eligible = sector.SecurityGuardSectors?
                .Where(l => l.SecurityGuard is { IsActive: true } && activeGuardIds.Contains(l.SecurityGuardId))
                .Select(l => l.SecurityGuardId)
                .Distinct()
                .OrderBy(id => id)
                .ToList() ?? [];

            sectorDefinitions.Add(new SectorWorkloadDefinition(
                sector.Id,
                sector.RequiredGuardsPerDay,
                eligible));
        }

        if (sectorDefinitions.Count == 0)
        {
            logger.LogWarning(
                "No active sectors configured with staffing requirements for {Month}/{Year}",
                request.Month,
                request.Year);
            return new GenerateMonthlyScheduleResult(GenerateMonthlyScheduleStatus.NoWorkloadSectorsConfigured);
        }

        var start = new DateOnly(request.Year, request.Month, 1);
        var end = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
        var unavailableRows =
            await unavailableDayRepository.GetByDateRangeAsync(start, end, cancellationToken);

        var unavailableByGuard = unavailableRows
            .GroupBy(x => x.SecurityGuardId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.Date).ToHashSet());

        var generator = new ScheduleGeneratorService();
        var generated = generator.Generate(request.Month, request.Year, sectorDefinitions, unavailableByGuard);

        if (!generated.Success)
        {
            logger.LogWarning(
                "Generation failed ({Reason}) for {Month}/{Year} on date {FailedDate}",
                generated.FailureReason,
                request.Month,
                request.Year,
                generated.FailedDate);

            if (generated.FailureReason is ScheduleGenerationFailureReason.NoConfiguredSectors
                or ScheduleGenerationFailureReason.NoEligibleGuardsForSectors)
            {
                return new GenerateMonthlyScheduleResult(GenerateMonthlyScheduleStatus.NoWorkloadSectorsConfigured);
            }

            return new GenerateMonthlyScheduleResult(
                GenerateMonthlyScheduleStatus.ImpossibleToGenerate,
                FailedDate: generated.FailedDate);
        }

        await monthlyScheduleRepository.AddAsync(generated.Schedule!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Monthly schedule {ScheduleId} generated for {Month}/{Year} with {ItemCount} shifts",
            generated.Schedule!.Id,
            request.Month,
            request.Year,
            generated.Schedule.Items.Count);

        return new GenerateMonthlyScheduleResult(GenerateMonthlyScheduleStatus.Success, generated.Schedule.Id);
    }
}
