using Ida.Application.Common.Crud;
using Ida.Domain.Bu;

namespace Ida.Application.Features.MasterData.Accounting;

public sealed class TaxTypeSpec : SimpleMasterSpec<MstTaxType>
{
    public override string Resource => "tax-types";
    public override string DisplayNameTh => "ประเภทภาษี";
    public override string Module => "master-data-accounting";
}

