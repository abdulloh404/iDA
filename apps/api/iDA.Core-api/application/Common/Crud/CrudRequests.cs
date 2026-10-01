using Ida.Domain.Common;

namespace Ida.Application.Common.Crud;

public record ListQuery<TEntity, TList>(ListRequest Request)
    : IQuery<PagedResult<TList>> where TEntity : class, IEntity, new();

public record GetByIdQuery<TEntity, TDetail>(Guid Id)
    : IQuery<TDetail> where TEntity : class, IEntity, new();

public record CreateCommand<TEntity, TDetail, TInput>(TInput Input)
    : ICommand<TDetail> where TEntity : class, IEntity, new();

public record UpdateCommand<TEntity, TDetail, TInput>(Guid Id, TInput Input, string? RowVersion)
    : ICommand<TDetail> where TEntity : class, IEntity, new();

public record DeleteCommand<TEntity>(Guid Id)
    : ICommand<Unit> where TEntity : class, IEntity, new();

public record HistoryQuery<TEntity>(Guid Id)
    : IQuery<IReadOnlyList<AuditEntryDto>> where TEntity : class, IEntity, new();

public record ExportQuery<TEntity, TList>(ListRequest Request)
    : IQuery<ExportFile> where TEntity : class, IEntity, new();

public record ExportFile(byte[] Content, string FileName)
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

public record Unit
{
    public static readonly Unit Value = new();
}

