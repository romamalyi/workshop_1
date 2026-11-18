using FluentResults;

using LeoHR.Server.Features.OnboardingTypes.Dto;

namespace LeoHR.Server.Features.OnboardingTypes;

public interface IOnboardingTypesService
{
    Task<OnboardingTypesDto> GetAll();

    Task<List<TypeLibraryTemplateDto>> GetLibrary();

    Task<Result<OnboardingTypeDetailDto>> GetById(int id);

    Task<Result<OnboardingTypeDto>> Create(SaveTypeRequest request);

    Task<Result<OnboardingTypeDto>> Update(int id, SaveTypeRequest request);

    Task<Result<OnboardingTypeDto>> Duplicate(int id);

    Task<Result<OnboardingTypeDto>> SetArchived(int id, bool isArchived);
}
