namespace LeoHR.Server.Shared.Models.Filter;

public record PagedResult<T>(List<T> Items, int TotalCount);
