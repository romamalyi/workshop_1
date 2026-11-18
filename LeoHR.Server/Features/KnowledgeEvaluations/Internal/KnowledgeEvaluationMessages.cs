namespace LeoHR.Server.Features.KnowledgeEvaluations.Internal;

public static class KnowledgeEvaluationMessages
{
    public const string Promotion = "Promotion";
    public const string KnowledgeEvaluation = "Knowledge evaluation";
    public const string Employee = "Employee";

    public const string NotYourPromotion = "You can only access your own promotions.";
    public const string PromotionSent = "A knowledge evaluation can't be planned for a sent promotion.";
    public const string NotRequiredForLevel = "The promotion's level doesn't require a knowledge evaluation.";
    public const string EvaluationDateInPast = "A planned knowledge evaluation can't be scheduled in the past.";
    public const string AlreadyMet = "The knowledge evaluation is already marked as met for this promotion.";
    public const string DateInPast = "The requested date can't be in the past.";
    public const string AlreadyPlanned = "A knowledge evaluation is already planned for this promotion.";
    public const string AlreadySuccessful = "The knowledge evaluation for this promotion is already successful.";
    public const string RequestLocked = "The request can only be changed while the evaluation is planned and has no date yet.";
    public const string ResultRecorded = "A knowledge evaluation with a recorded result can't be edited.";
    public const string InvalidLink = "The link must be a valid http or https address.";
}
