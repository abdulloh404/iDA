using System.Linq.Expressions;
using Ida.Domain.Common;

namespace Ida.Application.Common.Crud;

public record LookupItem(string Id, string Code, string Name);

public record LookupQuery<TEntity>(ListRequest Request)
    : IQuery<IReadOnlyList<LookupItem>> where TEntity : class, IEntity, new();

public class CrudLookupHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec)
    : IQueryHandler<LookupQuery<TEntity>, IReadOnlyList<LookupItem>>
    where TEntity : class, IEntity, new()
{

    public const int MaxOptions = 500;

    public async Task<IReadOnlyList<LookupItem>> Handle(LookupQuery<TEntity> query,
        CancellationToken ct)
    {
        if (spec.LookupProjection is not { } projection)
            throw ApiException.BadRequest("lookup_not_supported",
                $"{spec.DisplayNameTh}ไม่ได้เปิดให้ใช้เป็นตัวเลือกของหน้าจออื่น");

        var request = query.Request;
        var q = spec.Search(repo.Query(), request);

        q = CrudQuery.ApplyStatusFilter(q, StatusFilter.Active);
        q = CrudQuery.ApplySort(q, spec.Sortable, request, spec.LookupSort);

        var rows = q.Take(MaxOptions).Select(projection);
        return await exec.ToListAsync(rows, ct);
    }
}

