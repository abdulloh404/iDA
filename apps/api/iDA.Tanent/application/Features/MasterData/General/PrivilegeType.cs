using Ida.Application.Common.Crud;
using Ida.Domain.Bu;

namespace Ida.Application.Features.MasterData.General;

public sealed class PrivilegeTypeSpec : SimpleMasterSpec<MstPrivilegeType>
{
    public override string Resource => "privilege-types";
    public override string DisplayNameTh => "Privilege Type";
    public override string Module => "master-data-general";

    public override string CodeLabelTh => "รหัส Privilege Type";
}

