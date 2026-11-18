using System.Net;
using System.Net.Http.Json;

using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Migrations.LeoHr;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class OpenPromotionUniquenessTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task Creating_promotions_for_one_employee_at_once_creates_one_and_rejects_the_rest_as_already_existing()
    {
        HttpClient hr = factory.ClientFor(TestUser.HrAdmin(factory.Employees.Add().Id));
        ApiEmployee employee = factory.Employees.Add(
            profile: ".NET Developer", seniority: "Junior", englishLevel: "Advanced");
        CreatePromotionRequest request = new(
            employee.Id, FromRole: string.Empty, PromotionScenario.TargetLevel, Technology.Dotnet, PromotionHolderId: string.Empty);

        HttpResponseMessage[] responses =
            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => hr.PostJson("api/promotions", request)));

        Assert.Single(responses, r => r.IsSuccessStatusCode);
        foreach (HttpResponseMessage rejected in responses.Where(r => !r.IsSuccessStatusCode))
        {
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            ErrorResponse body = (await rejected.Content.ReadFromJsonAsync<ErrorResponse>(HttpClientJson.Options))!;
            Assert.Contains(body.Errors, e => e.ErrorCode == "DUPLICATE_RESOURCE" && e.ErrorMessage.Contains("already exists"));
        }
        Assert.Equal(1, await CountOpenPromotions(employee.Id));
    }

    [Fact]
    public async Task The_database_rejects_a_second_open_promotion_for_the_same_employee()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        PromotionEntity existing = await scenario.GetStoredPromotion();

        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        db.Promotions.Add(new PromotionEntity
        {
            EmployeeId = existing.EmployeeId,
            PromotionHolderId = existing.PromotionHolderId,
            FromRole = existing.FromRole,
            ToRole = existing.ToRole,
            Status = PromotionStatus.Ready,
        });

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(exception.IsUniqueViolation(LeoHrDbContext.OpenPromotionIndex));
    }

    [Fact]
    public async Task The_migration_keeps_the_Ready_duplicate_and_soft_deletes_the_other_open_ones()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);

        List<PromotionEntity> rows = await ResolveDuplicates(scenario, [PromotionStatus.InProgress, PromotionStatus.Ready]);

        PromotionEntity kept = Assert.Single(rows, p => !p.IsDeleted);
        Assert.Equal(PromotionStatus.Ready, kept.Status);
    }

    [Fact]
    public async Task Without_a_Ready_duplicate_the_migration_keeps_the_oldest_open_promotion()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);

        List<PromotionEntity> rows = await ResolveDuplicates(scenario, [PromotionStatus.InProgress, PromotionStatus.InProgress]);

        PromotionEntity kept = Assert.Single(rows, p => !p.IsDeleted);
        Assert.Equal(scenario.PromotionId, kept.Id);
    }

    // Runs in a rolled-back transaction: the index is dropped to let duplicates in, as on a database migrated before it existed.
    private async Task<List<PromotionEntity>> ResolveDuplicates(PromotionScenario scenario, PromotionStatus[] extraStatuses)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync($"DROP INDEX {LeoHrDbContext.OpenPromotionIndex}");

        PromotionEntity existing = await db.Promotions.AsNoTracking().SingleAsync(p => p.Id == scenario.PromotionId);
        foreach (PromotionStatus status in extraStatuses)
        {
            db.Promotions.Add(new PromotionEntity
            {
                EmployeeId = existing.EmployeeId,
                PromotionHolderId = existing.PromotionHolderId,
                FromRole = existing.FromRole,
                ToRole = existing.ToRole,
                Status = status,
            });
        }
        await db.SaveChangesAsync();

        int softDeleted = await db.Database.ExecuteSqlRawAsync(AddOpenPromotionUniqueIndex.SoftDeleteDuplicateOpenPromotionsSql);

        Assert.Equal(extraStatuses.Length, softDeleted);
        return await db.Promotions.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.EmployeeId == existing.EmployeeId)
            .ToListAsync();
    }

    private async Task<int> CountOpenPromotions(string employeeId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        return await db.Promotions.CountAsync(p => p.EmployeeId == employeeId
            && (p.Status == PromotionStatus.InProgress || p.Status == PromotionStatus.Ready));
    }
}
