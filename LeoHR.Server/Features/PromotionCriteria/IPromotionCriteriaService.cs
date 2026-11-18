using FluentResults;

using LeoHR.Server.Shared;
using LeoHR.Server.Shared.PromotionCriteria;

namespace LeoHR.Server.Features.PromotionCriteria;

public interface IPromotionCriteriaService
{
    Task<List<PromotionCriteriaDto>> GetAll(string? technology, bool includeInactive);
    Task<Result<PromotionCriteriaDto>> GetById(int id);
    Task<Result<PromotionCriteriaDto>> Create(PromotionCriteriaRequest request);
    Task<Result<PromotionCriteriaDto>> Update(int id, PromotionCriteriaRequest request, EditMode mode);
    Task<Result<PromotionCriteriaDto>> Reactivate(int id);
    Task<Result> Delete(int id);
}
