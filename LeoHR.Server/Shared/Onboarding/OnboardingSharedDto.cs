namespace LeoHR.Server.Shared.Onboarding;

public record EmployeeCandidateDto(string Id, string FullName);

public record ResponsibleCandidateDto(string Id, string FullName, string? Position);

public record ResponsibleRoleOptionDto(string Key, string Name);

public record AffectedTypeDto(int Id, string Name);
