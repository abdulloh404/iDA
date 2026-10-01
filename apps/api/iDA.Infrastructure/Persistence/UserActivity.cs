using Ida.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ida.Infrastructure.Persistence;

public class UserActivity(IdaDbContext db) : IUserActivity
{
    public Task MarkLoginAsync(Guid userId, DateTimeOffset at, CancellationToken ct) =>
        db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, at), ct);
}
