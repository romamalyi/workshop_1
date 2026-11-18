using FluentResults;

namespace LeoHR.Server.Shared.Results.Errors;

public class BusinessRuleError : Error
{
    public BusinessRuleError(string message, string? propertyName = null)
        : base(message)
    {
        if (!string.IsNullOrEmpty(propertyName))
        {
            Metadata.Add("PropertyName", propertyName);
        }
    }
}

public class ForbiddenError(string message) : Error(message);

public class ServiceUnavailableError(string message) : Error(message);

public class DuplicateResourceError : Error
{
    private const string ErrorCode = "DUPLICATE_RESOURCE";

    public DuplicateResourceError(string message) : base(message)
    {
        Metadata.Add("ErrorCode", ErrorCode);
    }

    public DuplicateResourceError(string resourceType, object resourceId, string? propertyName = null)
        : base(string.IsNullOrEmpty(propertyName)
            ? $"{resourceType} '{resourceId}' already exists."
            : $"{resourceType} with {propertyName} '{resourceId}' already exists.")
    {
        Metadata.Add("ErrorCode", ErrorCode);
        Metadata.Add("ResourceType", resourceType);
        Metadata.Add("ResourceId", resourceId.ToString()!);
        if (!string.IsNullOrEmpty(propertyName))
        {
            Metadata.Add("PropertyName", propertyName);
        }
    }
}

public class NotFoundError : Error
{
    public NotFoundError(string resourceType, object resourceId, string? propertyName = null)
        : base($"{resourceType} with id {resourceId} not found.")
    {
        Metadata.Add("ResourceType", resourceType);
        Metadata.Add("ResourceId", resourceId.ToString()!);
        if (!string.IsNullOrEmpty(propertyName))
        {
            Metadata.Add("PropertyName", propertyName);
        }
    }
}
