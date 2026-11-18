namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionMessages
{
    public const string Promotion = "Promotion";
    public const string Activity = "Activity";
    public const string Employee = "Employee";

    public const string NotYourPromotion = "You can only access your own promotions.";

    public const string PromotionNotInProgress = "Activities can only be added or removed while the promotion is in progress.";
    public const string PromotionNotReadyToSend = "Only promotions in the Ready to Send status can be sent.";
    public const string CriteriaNotMet = "Every criterion must be met before the promotion can be sent.";
    public const string KePlannedBlocksSend = "A knowledge evaluation is still planned. Mark it Successful or Unsuccessful before sending.";
    public const string ActivityScoreAutomatic = "\"Activity score required\" is set from the approved activities and can't be changed by hand.";
    public const string ActivityAlreadyAdded = "This activity has already been added to the promotion.";
    public const string SentPromotionNotEditable = "A sent promotion can no longer be edited.";
    public const string KeSuccessfulLocked = "\"KE required\" can't be unticked while the promotion has a successful knowledge evaluation.";
    public const string SentPromotionReadOnly = "The criteria and activities of a sent promotion can't be changed.";
    public const string ExtraCcNotFound = "One or more of the added CC recipients could not be found or has no email address.";
    public const string HolderIsPromotedEmployee = "The promotion holder can't be the employee being promoted.";
    public const string LevelNotAvailable = "Choose a level above the current one.";
    public const string NoCriteriaForLevel = "No criteria are set up for this level. Add criteria first.";
    public const string UnknownCriterion = "This criterion isn't part of the promotion's criteria.";
    public const string LevelAlreadyReached = "The employee has already been promoted to this level.";
    public const string NoPromotionTrack = "The employee's specialization has no promotion track.";
    public const string NotEmployeesTrack = "A promotion can only be created on the employee's own track.";
    public const string CurrentLevelUnknown = "The employee's current level isn't recognised, so no promotion level can be offered.";
}
