using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Features.Notifications.Dto;

/// <param name="EventType">What happened. Supported values are listed in the server README.</param>
/// <param name="EmployeeId">Employee API id of the employee the event is about.</param>
/// <param name="EventDescription">Free text from the caller, e.g. "English level changed from B1 to B2".</param>
/// <param name="Parameters">Template values for event types with their own email; the keys per type are listed in the server README.</param>
public record EmployeeEventRequest(
    [Required, EnumDataType(typeof(EmployeeEventType))] EmployeeEventType? EventType,
    [Required] string EmployeeId,
    [Required, MaxLength(1000)] string EventDescription,
    [MaxLength(20)] Dictionary<string, string>? Parameters = null
);
