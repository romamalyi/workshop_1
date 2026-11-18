using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace LeoHR.Server.Shared.Data;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException exception, string indexName)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            && postgres.ConstraintName == indexName;
    }
}
