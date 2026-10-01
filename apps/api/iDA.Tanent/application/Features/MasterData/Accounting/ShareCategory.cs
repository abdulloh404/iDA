using Ida.Application.Common.Crud;
using Ida.Domain.Bu;

namespace Ida.Application.Features.MasterData.Accounting;

public sealed class ShareCategorySpec : SimpleMasterSpec<MstShareCategory>
{
    public override string Resource => "share-categories";
    public override string DisplayNameTh => "ประเภทส่วนแบ่ง";
    public override string Module => "master-data-accounting";
}

