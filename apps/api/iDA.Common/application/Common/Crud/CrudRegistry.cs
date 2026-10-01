using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Ida.Application.Common.Crud;

public record CrudResource(
    string Name,
    string DisplayNameTh,
    string Module,
    bool IsGroupLevel,
    IReadOnlyList<string> FilterKeys,
    bool SupportsExport,
    bool SupportsLookup,
    Type EntityType,
    Type ListType,
    Type DetailType,
    Type InputType)
{
    public static readonly string[] Actions = ["read", "write", "delete", "export"];

    public string Permission(string action) => $"{Name}.{action}";
}

public static class CrudRegistry
{

    public static IReadOnlyList<CrudResource> Resources { get; private set; } = [];

    public static IServiceCollection AddCrudResources(this IServiceCollection services,
        Assembly assembly)
    {
        var resources = new List<CrudResource>();

        foreach (var specType in assembly.GetTypes())
        {
            if (specType.IsAbstract || !specType.IsClass) continue;
            if (BaseCrudSpec(specType) is not { } baseType) continue;

            var args = baseType.GetGenericArguments();
            var (entity, list, detail, input) = (args[0], args[1], args[2], args[3]);

            services.AddScoped(baseType, specType);

            Register(services,
                typeof(ListQuery<,>).MakeGenericType(entity, list),
                typeof(PagedResult<>).MakeGenericType(list),
                typeof(CrudListHandler<,,,>), args);

            Register(services,
                typeof(GetByIdQuery<,>).MakeGenericType(entity, detail),
                detail,
                typeof(CrudGetByIdHandler<,,,>), args);

            Register(services,
                typeof(CreateCommand<,,>).MakeGenericType(entity, detail, input),
                detail,
                typeof(CrudCreateHandler<,,,>), args);

            Register(services,
                typeof(UpdateCommand<,,>).MakeGenericType(entity, detail, input),
                detail,
                typeof(CrudUpdateHandler<,,,>), args);

            Register(services,
                typeof(DeleteCommand<>).MakeGenericType(entity),
                typeof(Unit),
                typeof(CrudDeleteHandler<,,,>), args);

            Register(services,
                typeof(HistoryQuery<>).MakeGenericType(entity),
                typeof(IReadOnlyList<AuditEntryDto>),
                typeof(CrudHistoryHandler<,,,>), args);

            Register(services,
                typeof(ExportQuery<,>).MakeGenericType(entity, list),
                typeof(ExportFile),
                typeof(CrudExportHandler<,,,>), args);

            Register(services,
                typeof(LookupQuery<>).MakeGenericType(entity),
                typeof(IReadOnlyList<LookupItem>),
                typeof(CrudLookupHandler<,,,>), args);

            if (Activator.CreateInstance(specType) is not ICrudSpecMeta meta)
                throw new InvalidOperationException(
                    $"{specType.Name} must have a parameterless constructor.");

            resources.Add(new CrudResource(
                meta.Resource, meta.DisplayNameTh, meta.Module, meta.IsGroupLevel,
                meta.FilterKeys, meta.HasExport, meta.HasLookup, entity, list, detail, input));
        }

        Resources = [.. resources.OrderBy(r => r.Module).ThenBy(r => r.Name)];
        return services;
    }

    private static void Register(IServiceCollection services,
        Type request, Type response, Type handlerDefinition, Type[] args)
    {
        var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(request, response);
        services.AddTransient(handlerInterface, handlerDefinition.MakeGenericType(args));
    }

    private static Type? BaseCrudSpec(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CrudSpec<,,,>))
                return t;
        return null;
    }
}

