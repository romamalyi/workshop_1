namespace LeoHR.Server.Shared.Models.Filter;

public record BaseFilterModel
(
    int? PageNumber = null,
    int? PageSize = null,
    string? SearchTerm = null
);
