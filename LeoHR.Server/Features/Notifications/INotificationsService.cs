using FluentResults;

using LeoHR.Server.Features.Notifications.Dto;

namespace LeoHR.Server.Features.Notifications;

public interface INotificationsService
{
    Task<Result> HandleEmployeeEvent(EmployeeEventRequest request);
}
