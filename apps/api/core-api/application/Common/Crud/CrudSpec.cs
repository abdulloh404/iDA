using System.Linq.Expressions;
using Ida.Domain.Common;

namespace Ida.Application.Common.Crud;

public abstract class CrudSpec<TEntity, TList, TDetail, TInput> : ICrudSpecMeta
    where TEntity : class, IEntity, new()
{

    public abstract string Resource { get; }

    public abstract string DisplayNameTh { get; }

    public virtual string Module => "master-data";

    public virtual bool IsGroupLevel => false;

    public abstract string DefaultSort { get; }

    public virtual string LookupSort => DefaultSort;

    public virtual bool CanCreate => true;

    public virtual bool IsSingleton => false;

    public abstract Expression<Func<TEntity, TList>> ListProjection { get; }

    public abstract Expression<Func<TEntity, TDetail>> DetailProjection { get; }

    public abstract IReadOnlyDictionary<string, Expression<Func<TEntity, object?>>> Sortable { get; }

    public virtual IQueryable<TEntity> Search(IQueryable<TEntity> query, ListRequest request) => query;

    public virtual Task<IQueryable<TEntity>> PrepareListQueryAsync(IQueryable<TEntity> query,
        ListRequest request, ICrudRelatedData related, CancellationToken ct) =>
        Task.FromResult(query);

    public virtual Task<IQueryable<TEntity>> ApplySortAsync(IQueryable<TEntity> query,
        ListRequest request, ICrudRelatedData related, CancellationToken ct) =>
        Task.FromResult(CrudQuery.ApplySort(query, Sortable, request, DefaultSort));

    public virtual IQueryable<TEntity> IncludeForDetail(IQueryable<TEntity> query) => query;

    public virtual Task<IReadOnlyList<TList>> EnrichListAsync(IReadOnlyList<TList> items,
        ICrudRelatedData related, CancellationToken ct) => Task.FromResult(items);

    public virtual Task<TDetail> EnrichDetailAsync(TDetail item, ICrudRelatedData related,
        CancellationToken ct) => Task.FromResult(item);

    public virtual IReadOnlyList<string> FilterKeys => [];

    public virtual Expression<Func<TEntity, LookupItem>>? LookupProjection => null;

    public virtual Expression<Func<TEntity, bool>>? UniqueCodeScope(TEntity entity) => null;

    public virtual string DuplicateCodeMessageTh => $"รหัส{DisplayNameTh}นี้ถูกใช้งานแล้ว";

    public abstract void Apply(TEntity entity, TInput input, bool isCreate);

    public virtual Task ValidateAsync(TEntity entity, TInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct) => Task.CompletedTask;

    public virtual Task ValidateAgainstDataAsync(TEntity entity, TInput input, bool isCreate,
        ValidationFailure errors, IRepository<TEntity> repo, IQueryExecutor exec,
        CancellationToken ct) => Task.CompletedTask;

    public virtual Task OnSavedAsync(TEntity entity, TInput input, bool isCreate,
        CancellationToken ct) => Task.CompletedTask;

    public virtual Task<string?> WhyCannotDeleteAsync(TEntity entity, CancellationToken ct) =>
        Task.FromResult<string?>(null);

    public virtual Task<string?> WhyCannotDeleteAgainstDataAsync(TEntity entity,
        IRepository<TEntity> repo, IQueryExecutor exec, CancellationToken ct) =>
        Task.FromResult<string?>(null);

    public virtual Task ValidateForActorAsync(TEntity entity, TInput input, bool isCreate,
        ValidationFailure errors, ICurrentUser actor, CancellationToken ct) => Task.CompletedTask;

    public virtual string? WhyActorCannotDelete(TEntity entity, ICurrentUser actor) => null;

    public virtual IReadOnlyList<ExcelColumn<TList>> ExportColumns => [];

    public virtual string ExportSheetName => DisplayNameTh;

    bool ICrudSpecMeta.HasExport => ExportColumns.Count > 0;

    bool ICrudSpecMeta.HasLookup => LookupProjection is not null;
}

public interface ICrudSpecMeta
{
    string Resource { get; }
    string DisplayNameTh { get; }
    string Module { get; }
    bool IsGroupLevel { get; }
    IReadOnlyList<string> FilterKeys { get; }
    bool HasExport { get; }
    bool HasLookup { get; }
}
