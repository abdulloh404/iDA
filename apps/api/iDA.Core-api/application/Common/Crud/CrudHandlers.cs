using System.Linq.Expressions;
using Ida.Domain.Common;

namespace Ida.Application.Common.Crud;

public class CrudListHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    ICrudRelatedData related)
    : IQueryHandler<ListQuery<TEntity, TList>, PagedResult<TList>>
    where TEntity : class, IEntity, new()
{
    public async Task<PagedResult<TList>> Handle(ListQuery<TEntity, TList> query, CancellationToken ct)
    {
        var request = query.Request;
        var q = spec.Search(repo.Query(), request);
        q = await spec.PrepareListQueryAsync(q, request, related, ct);
        q = CrudQuery.ApplyStatusFilter(q, request.Status);

        var total = await exec.CountAsync(q, ct);

        q = await spec.ApplySortAsync(q, request, related, ct);
        var page = q.Skip(request.Skip).Take(request.PageSize).Select(spec.ListProjection);

        var items = await exec.ToListAsync(page, ct);
        items = [.. await spec.EnrichListAsync(items, related, ct)];
        return new PagedResult<TList>(items, request.Page, request.PageSize, total);
    }
}

public class CrudGetByIdHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    ICrudRelatedData related)
    : IQueryHandler<GetByIdQuery<TEntity, TDetail>, TDetail>
    where TEntity : class, IEntity, new()
{
    public async Task<TDetail> Handle(GetByIdQuery<TEntity, TDetail> query, CancellationToken ct)
    {
        var q = spec.IncludeForDetail(repo.Query()).Where(e => e.Id == query.Id);
        var dto = await exec.FirstOrDefaultAsync(q.Select(spec.DetailProjection), ct);

        var found = dto ?? throw ApiException.NotFound(
            $"{spec.Resource}_not_found", $"ไม่พบข้อมูล{spec.DisplayNameTh}ที่ระบุ");
        return await spec.EnrichDetailAsync(found, related, ct);
    }
}

public class CrudCreateHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser actor,
    ICrudRelatedData related)
    : ICommandHandler<CreateCommand<TEntity, TDetail, TInput>, TDetail>
    where TEntity : class, IEntity, new()
{
    public async Task<TDetail> Handle(CreateCommand<TEntity, TDetail, TInput> command, CancellationToken ct)
    {
        if (!spec.CanCreate)
            throw ApiException.BadRequest("create_not_allowed",
                $"รายการ{spec.DisplayNameTh}กำหนดโดยระบบ เพิ่มเองไม่ได้ แก้ไขได้เฉพาะรายการที่มีอยู่");

        if (spec.IsSingleton && await exec.AnyAsync(repo.Query(), ct))
            throw ApiException.Conflict("already_configured",
                $"มีการ{spec.DisplayNameTh}อยู่แล้ว ให้แก้ไขรายการเดิม");

        var entity = new TEntity();
        spec.Apply(entity, command.Input, isCreate: true);

        var errors = new ValidationFailure();
        await spec.ValidateAsync(entity, command.Input, isCreate: true, errors, ct);
        await spec.ValidateAgainstDataAsync(entity, command.Input, isCreate: true, errors,
            repo, exec, ct);
        await spec.ValidateForActorAsync(entity, command.Input, isCreate: true, errors, actor, ct);
        errors.ThrowIfInvalid();
        await CrudQuery.CheckDuplicateCodeAsync(entity, repo, exec,
            spec.UniqueCodeScope(entity), spec.DuplicateCodeMessageTh, ct);

        repo.Add(entity);
        await uow.SaveChangesAsync(ct);

        await spec.OnSavedAsync(entity, command.Input, isCreate: true, ct);
        await uow.SaveChangesAsync(ct);

        return await CrudQuery.LoadDetailAsync(spec, repo, exec, related, entity.Id, ct);
    }
}

public class CrudUpdateHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser actor,
    ICrudRelatedData related)
    : ICommandHandler<UpdateCommand<TEntity, TDetail, TInput>, TDetail>
    where TEntity : class, IEntity, new()
{
    public async Task<TDetail> Handle(UpdateCommand<TEntity, TDetail, TInput> command, CancellationToken ct)
    {
        var entity = await exec.FirstOrDefaultAsync(
            repo.Track().Where(e => e.Id == command.Id), ct)
            ?? throw ApiException.NotFound(
                $"{spec.Resource}_not_found", $"ไม่พบข้อมูล{spec.DisplayNameTh}ที่ระบุ");

        if (!string.IsNullOrEmpty(command.RowVersion))
            repo.SetConcurrencyToken(entity, command.RowVersion);

        spec.Apply(entity, command.Input, isCreate: false);

        var errors = new ValidationFailure();
        await spec.ValidateAsync(entity, command.Input, isCreate: false, errors, ct);
        await spec.ValidateAgainstDataAsync(entity, command.Input, isCreate: false, errors,
            repo, exec, ct);
        await spec.ValidateForActorAsync(entity, command.Input, isCreate: false, errors, actor, ct);
        errors.ThrowIfInvalid();
        await CrudQuery.CheckDuplicateCodeAsync(entity, repo, exec,
            spec.UniqueCodeScope(entity), spec.DuplicateCodeMessageTh, ct);

        await uow.SaveChangesAsync(ct);

        await spec.OnSavedAsync(entity, command.Input, isCreate: false, ct);
        await uow.SaveChangesAsync(ct);

        return await CrudQuery.LoadDetailAsync(spec, repo, exec, related, entity.Id, ct);
    }
}

public class CrudDeleteHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    IUnitOfWork uow,
    IReferenceGuard references,
    ICurrentUser actor)
    : ICommandHandler<DeleteCommand<TEntity>, Unit>
    where TEntity : class, IEntity, new()
{
    public async Task<Unit> Handle(DeleteCommand<TEntity> command, CancellationToken ct)
    {
        var entity = await exec.FirstOrDefaultAsync(
            repo.Track().Where(e => e.Id == command.Id), ct)
            ?? throw ApiException.NotFound(
                $"{spec.Resource}_not_found", $"ไม่พบข้อมูล{spec.DisplayNameTh}ที่ระบุ");

        if (!spec.CanCreate)
            throw ApiException.EntityInUse(
                $"รายการ{spec.DisplayNameTh}กำหนดโดยระบบ ลบไม่ได้ แก้ไขได้เฉพาะรายการที่มีอยู่");
        if (spec.IsSingleton)
            throw ApiException.EntityInUse(
                $"การ{spec.DisplayNameTh}ลบไม่ได้ แก้ไขค่าแทนได้");
        if (spec.WhyActorCannotDelete(entity, actor) is { } byActor)
            throw ApiException.EntityInUse(byActor);

        if (await spec.WhyCannotDeleteAsync(entity, ct) is { } reason)
            throw ApiException.EntityInUse(reason);

        if (await spec.WhyCannotDeleteAgainstDataAsync(entity, repo, exec, ct) is { } byData)
            throw ApiException.EntityInUse(byData);

        if (await references.WhyCannotDeleteAsync(entity, ct) is { } referenced)
            throw ApiException.EntityInUse(referenced);

        repo.Remove(entity);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class CrudHistoryHandler<TEntity, TList, TDetail, TInput>(IAuditTrail audit)
    : IQueryHandler<HistoryQuery<TEntity>, IReadOnlyList<AuditEntryDto>>
    where TEntity : class, IEntity, new()
{
    public Task<IReadOnlyList<AuditEntryDto>> Handle(HistoryQuery<TEntity> query, CancellationToken ct) =>
        audit.ForAsync(typeof(TEntity).Name, query.Id.ToString(), ct);
}

public class CrudExportHandler<TEntity, TList, TDetail, TInput>(
    CrudSpec<TEntity, TList, TDetail, TInput> spec,
    IRepository<TEntity> repo,
    IQueryExecutor exec,
    IExcelWriter excel,
    IClock clock,
    ICrudRelatedData related)
    : IQueryHandler<ExportQuery<TEntity, TList>, ExportFile>
    where TEntity : class, IEntity, new()
{
    public async Task<ExportFile> Handle(ExportQuery<TEntity, TList> query, CancellationToken ct)
    {
        if (spec.ExportColumns.Count == 0)
            throw ApiException.BadRequest("export_not_supported",
                $"ยังไม่ได้กำหนดรูปแบบไฟล์ Export ของ{spec.DisplayNameTh}");

        var q = spec.Search(repo.Query(), query.Request);
        q = await spec.PrepareListQueryAsync(q, query.Request, related, ct);
        q = CrudQuery.ApplyStatusFilter(q, query.Request.Status);
        q = await spec.ApplySortAsync(q, query.Request, related, ct);

        var rows = await exec.ToListAsync(q.Select(spec.ListProjection), ct);
        rows = [.. await spec.EnrichListAsync(rows, related, ct)];
        var bytes = excel.Write(rows, spec.ExportColumns, spec.ExportSheetName);
        var stamp = clock.Now.ToString("yyyyMMdd-HHmm");
        return new ExportFile(bytes, $"{spec.Resource}-{stamp}.xlsx");
    }
}

internal static class CrudQuery
{

    public static IQueryable<TEntity> ApplyStatusFilter<TEntity>(
        IQueryable<TEntity> query, StatusFilter filter) where TEntity : class
    {
        if (filter == StatusFilter.All) return query;
        if (typeof(TEntity).GetProperty("Status")?.PropertyType != typeof(RecordStatus))
            return query;

        var wanted = filter == StatusFilter.Active ? RecordStatus.Active : RecordStatus.Inactive;
        var param = Expression.Parameter(typeof(TEntity), "e");
        var body = Expression.Equal(
            Expression.Property(param, "Status"),
            Box(wanted));
        return query.Where(Expression.Lambda<Func<TEntity, bool>>(body, param));
    }

    public static IQueryable<TEntity> ApplySort<TEntity>(
        IQueryable<TEntity> query,
        IReadOnlyDictionary<string, Expression<Func<TEntity, object?>>> sortable,
        ListRequest request,
        string defaultSort) where TEntity : class
    {
        var (key, descending) = request.ParseSort(defaultSort);

        if (!sortable.TryGetValue(key, out var selector))
            throw ApiException.BadRequest("unknown_sort",
                $"ไม่รองรับการเรียงลำดับด้วยคอลัมน์ '{key}'",
                new { allowed = sortable.Keys.ToArray() });

        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    public static async Task CheckDuplicateCodeAsync<TEntity>(
        TEntity entity, IRepository<TEntity> repo, IQueryExecutor exec,
        Expression<Func<TEntity, bool>>? scope, string message, CancellationToken ct)
        where TEntity : class, IEntity
    {
        if (entity is not ICodedEntity coded || string.IsNullOrWhiteSpace(coded.Code)) return;

        var param = Expression.Parameter(typeof(TEntity), "e");
        var sameCode = Expression.Equal(
            Expression.Property(param, nameof(ICodedEntity.Code)),
            Box(coded.Code.Trim()));
        var otherRow = Expression.NotEqual(
            Expression.Property(param, nameof(IEntity<Guid>.Id)),
            Box(entity.Id));
        var rows = scope is null ? repo.Query() : repo.Query().Where(scope);
        var clash = rows.Where(Expression.Lambda<Func<TEntity, bool>>(
            Expression.AndAlso(sameCode, otherRow), param));

        if (await exec.AnyAsync(clash, ct))
            throw ApiException.DuplicateCode("code", message);
    }

    private static MemberExpression Box<T>(T value) =>
        Expression.Field(Expression.Constant(new ValueBox<T>(value)), nameof(ValueBox<T>.Value));

    private sealed class ValueBox<T>(T value)
    {
        public readonly T Value = value;
    }

    public static async Task<TDetail> LoadDetailAsync<TEntity, TList, TDetail, TInput>(
        CrudSpec<TEntity, TList, TDetail, TInput> spec,
        IRepository<TEntity> repo, IQueryExecutor exec, ICrudRelatedData related, Guid id,
        CancellationToken ct)
        where TEntity : class, IEntity, new()
    {
        var q = spec.IncludeForDetail(repo.Query()).Where(e => e.Id == id);
        var detail = await exec.FirstOrDefaultAsync(q.Select(spec.DetailProjection), ct)
            ?? throw ApiException.NotFound(
                $"{spec.Resource}_not_found", $"ไม่พบข้อมูล{spec.DisplayNameTh}ที่ระบุ");
        return await spec.EnrichDetailAsync(detail, related, ct);
    }
}
