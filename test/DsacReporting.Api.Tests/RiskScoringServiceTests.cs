using DsacReporting.Api.Data;
using DsacReporting.Api.Data.Entities;
using DsacReporting.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DsacReporting.Api.Tests;

public class RiskScoringServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static (ReportingCycle Prior1, ReportingCycle Prior2, ReportingCycle Prior3, ReportingCycle Current)
        SeedFourCycles(AppDbContext db)
    {
        var prior1 = new ReportingCycle { Id = Guid.NewGuid(), Label = "Q1", DueDate = new DateOnly(2026, 3, 31) };
        var prior2 = new ReportingCycle { Id = Guid.NewGuid(), Label = "Q2", DueDate = new DateOnly(2026, 6, 30) };
        var prior3 = new ReportingCycle { Id = Guid.NewGuid(), Label = "Q3", DueDate = new DateOnly(2026, 9, 30) };
        var current = new ReportingCycle { Id = Guid.NewGuid(), Label = "Q4", DueDate = new DateOnly(2026, 12, 31) };
        db.ReportingCycles.AddRange(prior1, prior2, prior3, current);
        return (prior1, prior2, prior3, current);
    }

    [Fact]
    public async Task ComputeAllAsync_FlagsPredictedRisk_WhenTwoOfLastThreeCyclesMissed()
    {
        using var db = CreateDb(nameof(ComputeAllAsync_FlagsPredictedRisk_WhenTwoOfLastThreeCyclesMissed));
        var entity = new Entity { Id = Guid.NewGuid(), Name = "Chronically Late Entity", Type = "public_entity" };
        db.Entities.Add(entity);

        var (prior1, prior2, prior3, current) = SeedFourCycles(db);

        // Missed 2 of the last 3 (prior1, prior2), submitted the most recent (prior3),
        // and the current cycle isn't due for months - current-cycle rules alone
        // would score this entity 0.
        db.Submissions.AddRange(
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior1.Id, Status = "missed" },
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior2.Id, Status = "missed" },
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior3.Id, Status = "submitted" });

        await db.SaveChangesAsync();

        var service = new RiskScoringService(db);
        var results = await service.ComputeAllAsync();

        var result = Assert.Single(results, r => r.EntityId == entity.Id);
        Assert.True(result.Score >= 25, $"Expected predictive risk boost, got score {result.Score}");
        Assert.Contains("Predicted risk", result.Reason);
    }

    [Fact]
    public async Task ComputeAllAsync_DoesNotFlagPredictedRisk_WhenOnlyOneOfLastThreeMissed()
    {
        using var db = CreateDb(nameof(ComputeAllAsync_DoesNotFlagPredictedRisk_WhenOnlyOneOfLastThreeMissed));
        var entity = new Entity { Id = Guid.NewGuid(), Name = "Mostly On Time Entity", Type = "public_entity" };
        db.Entities.Add(entity);

        var (prior1, prior2, prior3, current) = SeedFourCycles(db);

        db.Submissions.AddRange(
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior1.Id, Status = "missed" },
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior2.Id, Status = "submitted" },
            new Submission { Id = Guid.NewGuid(), EntityId = entity.Id, CycleId = prior3.Id, Status = "submitted" });

        await db.SaveChangesAsync();

        var service = new RiskScoringService(db);
        var results = await service.ComputeAllAsync();

        var result = Assert.Single(results, r => r.EntityId == entity.Id);
        Assert.DoesNotContain("Predicted risk", result.Reason ?? string.Empty);
    }

    [Fact]
    public async Task ComputeAllAsync_PersistsScoresToRiskScoresTable()
    {
        using var db = CreateDb(nameof(ComputeAllAsync_PersistsScoresToRiskScoresTable));
        var entity = new Entity { Id = Guid.NewGuid(), Name = "Any Entity", Type = "public_entity" };
        db.Entities.Add(entity);
        var (_, _, _, current) = SeedFourCycles(db);
        await db.SaveChangesAsync();

        var service = new RiskScoringService(db);
        await service.ComputeAllAsync();

        var persisted = await db.RiskScores.SingleAsync(r => r.EntityId == entity.Id && r.CycleId == current.Id);
        Assert.Equal(0, persisted.Score);
    }
}
