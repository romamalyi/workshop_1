using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.Shared.Promotions;

public static class PromotionHolderRecipient
{
    public static async Task<Dictionary<RecipientRole, Recipient>> GetPromotionHolderRoleAsync(
        this IEmployeeLookup employeeLookup, string holderId)
    {
        EmployeeRef? holder = string.IsNullOrWhiteSpace(holderId) ? null : await employeeLookup.GetByIdAsync(holderId);
        return holder is null
            ? []
            : new() { [RecipientRole.PromotionHolder] = new Recipient($"{holder.FirstName} {holder.LastName}".Trim(), holder.Email) };
    }
}
