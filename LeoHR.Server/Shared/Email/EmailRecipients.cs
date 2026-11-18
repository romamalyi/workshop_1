namespace LeoHR.Server.Shared.Email;

public record Recipient(string Name, string Email);

// Listed once even when several of the template's roles resolve to the same address.
public record EmailRecipient(string Name, string Email, IReadOnlyList<RecipientRole> Roles);

public record EmailRecipients(
    IReadOnlyList<EmailRecipient> To,
    IReadOnlyList<EmailRecipient> Cc,
    IReadOnlyList<RecipientRole> Unresolved);
