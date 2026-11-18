using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Promotions.Internal;

public record PromotionLevel(Technology Technology, Seniority Seniority, SubLevel SubLevel);
