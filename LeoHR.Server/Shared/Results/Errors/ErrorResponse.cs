namespace LeoHR.Server.Shared.Results.Errors;

public class ErrorResponse
{
    public List<ErrorDetail> Errors { get; set; } = [];
}

public class ErrorDetail
{
    public string ErrorMessage { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? ErrorCode { get; set; } = null;
}
