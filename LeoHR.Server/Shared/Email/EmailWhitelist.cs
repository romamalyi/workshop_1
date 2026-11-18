using LeoHR.Server.Shared.Data;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Shared.Email;

public class EmailWhitelist(LeoHrDbContext db) : IEmailWhitelist
{
    public async Task<IReadOnlySet<string>> GetAllowedAsync(CancellationToken cancellationToken = default)
    {
        List<string> emails = await db.EmailWhitelist.Select(w => w.Email).ToListAsync(cancellationToken);
        return emails.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
