using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using PadelMatch.Application;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class DatabaseReadiness(PadelMatchDbContext dbContext) : IDatabaseReadiness
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return false;
            }

            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            return !pending.Any();
        }
        catch (DbException)
        {
            return false;
        }
    }
}
