using DentalClinic.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Extensions;

internal static class QueryableExtensions
{
    /// <summary>דפדוף אמיתי מול בסיס הנתונים: COUNT ואחריו Skip/Take בתוך השאילתה (לא בזיכרון).</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
