namespace LeoHR.Server.Shared.Models.Filter;

public record EmployeeFilterModel(
    List<string>? Seniorities = null,
    List<string>? Profiles = null,
    List<string>? Projects = null,
    List<string>? HrManagers = null
) : BaseFilterModel;
