using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;

namespace Ida.Application.Features.Doctors;

public abstract class DoctorChildSpec<TEntity, TList, TDetail, TInput>
    : CrudSpec<TEntity, TList, TDetail, TInput>
    where TEntity : class, IEntity, new()
{
    public override string Module => "doctors";
    public override bool IsGroupLevel => true;
    public override IReadOnlyList<string> FilterKeys => ["doctorId"];

    protected static Guid? DoctorIdFilter(ListRequest r) =>
        r.Filter("doctorId") is { } id && Guid.TryParse(id, out var parsed) ? parsed : null;
}

