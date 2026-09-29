using Ida.Application.Common.Crud;
using Ida.Domain.Bu;

namespace Ida.Application.Features.MasterData.General;

public sealed class StatusPrivilegeSpec : SimpleMasterSpec<MstStatusPrivilege>
{
    public override string Resource => "status-privileges";
    public override string DisplayNameTh => "Status Privilege";
    public override string Module => "master-data-general";

    public override string CodeLabelTh => "รหัส Privilege";
}

