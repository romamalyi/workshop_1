using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingTypes;

public class OnboardingTypeArchiveService(LeoHrDbContext db) : IOnboardingTypeArchive
{
    public Task<List<AffectedTypeDto>> GetTypesLeftEmptyBy(int templateId)
    {
        return TypesLeftEmptyBy(templateId)
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new AffectedTypeDto(t.Id, t.Name))
            .ToListAsync();
    }

    // A direct update skips the audit stamp, like archive and restore: this must not move "last edited" either.
    public Task ArchiveTypesLeftEmptyBy(int templateId)
    {
        return TypesLeftEmptyBy(templateId).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsArchived, true));
    }

    private IQueryable<OnboardingTypeEntity> TypesLeftEmptyBy(int templateId)
    {
        return db.OnboardingTypes.Where(t => !t.IsArchived
            && t.Templates.Any(x => x.TemplateId == templateId && x.IsEnabled && x.Template.IsActive)
            && !t.Templates.Any(x => x.TemplateId != templateId && x.IsEnabled && x.Template.IsActive));
    }
}
