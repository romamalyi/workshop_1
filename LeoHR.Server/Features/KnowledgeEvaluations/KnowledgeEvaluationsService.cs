using System.Globalization;
using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.KnowledgeEvaluations.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Promotions;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace LeoHR.Server.Features.KnowledgeEvaluations;

public class KnowledgeEvaluationsService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IPromotionKnowledgeEvaluation promotions,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings,
    ILogger<KnowledgeEvaluationsService> logger) : IKnowledgeEvaluationsService
{
    public async Task<Result<List<KnowledgeEvaluationDto>>> GetAll()
    {
        List<KnowledgeEvaluationEntity> rows = await db.KnowledgeEvaluations
            .AsNoTracking()
            .Include(e => e.Promotion)
            .OrderBy(e => e.Status == KnowledgeEvaluationStatus.Planned ? 0 : 1)
            .ThenBy(e => e.RequestedDate)
            .ThenBy(e => e.Id)
            .ToListAsync();
        return Result.Ok(await ToDtos(rows));
    }

    public async Task<Result<KnowledgeEvaluationDto?>> GetLatestForPromotion(int promotionId, ClaimsPrincipal user)
    {
        PromotionKeInfo? info = await promotions.GetInfoAsync(promotionId);
        if (info is null) return Result.Fail(new NotFoundError(KnowledgeEvaluationMessages.Promotion, promotionId));
        if (!user.CanReadManage() && !await employeeLookup.IsInManagementLineAsync(info.EmployeeId, user))
            return Result.Fail(new ForbiddenError(KnowledgeEvaluationMessages.NotYourPromotion));

        KnowledgeEvaluationEntity? latest = await db.KnowledgeEvaluations
            .AsNoTracking()
            .Include(e => e.Promotion)
            .Where(e => e.PromotionId == promotionId)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();
        return Result.Ok(latest is null ? null : await ToDto(latest));
    }

    public async Task<Result<KnowledgeEvaluationDto>> Create(
        int promotionId, RequestKnowledgeEvaluationRequest request, ClaimsPrincipal user)
    {
        PromotionKeInfo? info = await promotions.GetInfoAsync(promotionId);
        if (info is null) return Result.Fail(new NotFoundError(KnowledgeEvaluationMessages.Promotion, promotionId));
        if (info.EmployeeId != user.GetUserId())
            return Result.Fail(new ForbiddenError(KnowledgeEvaluationMessages.NotYourPromotion));
        if (info.IsSent) return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.PromotionSent));
        if (!info.RequiresKe) return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.NotRequiredForLevel));
        if (request.RequestedDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.DateInPast));

        List<KnowledgeEvaluationStatus> existing = await db.KnowledgeEvaluations
            .Where(e => e.PromotionId == promotionId)
            .Select(e => e.Status)
            .ToListAsync();
        if (existing.Contains(KnowledgeEvaluationStatus.Successful))
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.AlreadySuccessful));
        if (existing.Contains(KnowledgeEvaluationStatus.Planned))
            return Result.Fail(new DuplicateResourceError(KnowledgeEvaluationMessages.AlreadyPlanned));
        if (info.KeTicked) return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.AlreadyMet));

        var evaluation = new KnowledgeEvaluationEntity
        {
            PromotionId = promotionId,
            RequestedDate = request.RequestedDate,
            RequestedOption = request.RequestedOption,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
        };
        db.KnowledgeEvaluations.Add(evaluation);
        await db.SaveChangesAsync();
        await EmailRequest(evaluation, info);

        await db.Entry(evaluation).Reference(e => e.Promotion).LoadAsync();
        return await ToDto(evaluation);
    }

    public async Task<Result<KnowledgeEvaluationDto>> UpdateRequest(
        int id, RequestKnowledgeEvaluationRequest request, ClaimsPrincipal user)
    {
        KnowledgeEvaluationEntity? evaluation = await db.KnowledgeEvaluations
            .Include(e => e.Promotion)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (evaluation is null)
            return Result.Fail(new NotFoundError(KnowledgeEvaluationMessages.KnowledgeEvaluation, id));
        if (evaluation.Promotion.EmployeeId != user.GetUserId())
            return Result.Fail(new ForbiddenError(KnowledgeEvaluationMessages.NotYourPromotion));
        if (evaluation.Status != KnowledgeEvaluationStatus.Planned || evaluation.EvaluationDate is not null)
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.RequestLocked));

        // Keeping a requested date that has since passed is fine, so a comment-only edit isn't blocked.
        if (request.RequestedDate < DateOnly.FromDateTime(DateTime.UtcNow)
            && request.RequestedDate != evaluation.RequestedDate)
        {
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.DateInPast));
        }

        evaluation.RequestedDate = request.RequestedDate;
        evaluation.RequestedOption = request.RequestedOption;
        evaluation.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        await db.SaveChangesAsync();

        return await ToDto(evaluation);
    }

    public async Task<Result<KnowledgeEvaluationDto>> Update(int id, UpdateKnowledgeEvaluationRequest request)
    {
        KnowledgeEvaluationEntity? evaluation = await db.KnowledgeEvaluations
            .Include(e => e.Promotion)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (evaluation is null)
            return Result.Fail(new NotFoundError(KnowledgeEvaluationMessages.KnowledgeEvaluation, id));
        // A recorded result is final: "KE required" was ticked from it and a new request is only allowed after Unsuccessful.
        if (evaluation.Status != KnowledgeEvaluationStatus.Planned)
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.ResultRecorded));

        // Keeping a date that has already passed is fine, so a status or link edit isn't blocked; picking a new one isn't.
        if (request.Status == KnowledgeEvaluationStatus.Planned
            && request.EvaluationDate is { } evaluationDate
            && evaluationDate < DateOnly.FromDateTime(DateTime.UtcNow)
            && evaluationDate != evaluation.EvaluationDate)
        {
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.EvaluationDateInPast));
        }

        string? link = string.IsNullOrWhiteSpace(request.Link) ? null : request.Link.Trim();
        if (link is not null && !IsWebLink(link))
            return Result.Fail(new BusinessRuleError(KnowledgeEvaluationMessages.InvalidLink));

        string? conductedById = string.IsNullOrWhiteSpace(request.ConductedById) ? null : request.ConductedById;
        if (conductedById is not null && await employeeLookup.GetByIdAsync(conductedById) is null)
            return Result.Fail(new NotFoundError(KnowledgeEvaluationMessages.Employee, conductedById));

        bool becameSuccessful = request.Status == KnowledgeEvaluationStatus.Successful;
        evaluation.Status = request.Status;
        evaluation.ConductedById = conductedById;
        evaluation.EvaluationDate = request.EvaluationDate;
        evaluation.Link = link;

        // The criteria tick is saved by the promotions side; both saves land together.
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        if (becameSuccessful) await promotions.TickKnowledgeEvaluationAsync(evaluation.PromotionId);
        await transaction.CommitAsync();

        return await ToDto(evaluation);
    }

    private async Task EmailRequest(KnowledgeEvaluationEntity evaluation, PromotionKeInfo info)
    {
        EmployeeRef? employee = await employeeLookup.GetByIdAsync(info.EmployeeId);
        if (employee is null) return;

        string date = evaluation.RequestedDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var parameters = new Dictionary<string, string>
        {
            ["firstName"] = employee.FirstName,
            ["lastName"] = employee.LastName,
            ["requestedDate"] = evaluation.RequestedOption == KnowledgeEvaluationOption.AnyTimeAfter
                ? $"any time after {date}"
                : $"on {date}",
            ["comment"] = evaluation.Comment ?? "—",
            ["promotionLink"] = emailSettings.Value.PromotionUrl(evaluation.PromotionId),
        };
        string holderId = string.IsNullOrEmpty(info.PromotionHolderId)
            ? AppSettingsEntity.DefaultPromotionHolder
            : info.PromotionHolderId;

        Result sent = await emailService.SendForEmployeeAsync(
            EmailTemplateCatalog.KnowledgeEvaluationRequested, employee.Id, parameters,
            await employeeLookup.GetPromotionHolderRoleAsync(holderId));
        if (sent.IsFailed)
        {
            logger.LogError("The knowledge evaluation request {KnowledgeEvaluationId} could not be emailed: {Reasons}",
                evaluation.Id, string.Join("; ", sent.Errors.Select(e => e.Message)));
        }
    }

    private static bool IsWebLink(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private async Task<KnowledgeEvaluationDto> ToDto(KnowledgeEvaluationEntity evaluation)
    {
        return (await ToDtos([evaluation]))[0];
    }

    private async Task<List<KnowledgeEvaluationDto>> ToDtos(List<KnowledgeEvaluationEntity> evaluations)
    {
        IEnumerable<string> ids = evaluations
            .Select(e => e.Promotion.EmployeeId)
            .Concat(evaluations.Where(e => e.ConductedById is not null).Select(e => e.ConductedById!))
            .Distinct();
        Dictionary<string, string> names = await employeeLookup.GetNamesByIdsAsync(ids);

        return [.. evaluations.Select(e => new KnowledgeEvaluationDto(
            e.Id,
            e.PromotionId,
            e.Promotion.EmployeeId,
            names.GetValueOrDefault(e.Promotion.EmployeeId, string.Empty),
            e.RequestedDate,
            e.RequestedOption,
            e.Comment,
            e.ConductedById,
            e.ConductedById is null ? null : names.GetValueOrDefault(e.ConductedById),
            e.EvaluationDate,
            e.Status,
            e.Link))];
    }
}
