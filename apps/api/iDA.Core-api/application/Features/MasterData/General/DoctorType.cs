using Ida.Application.Common.Crud;
using Ida.Domain.Bu;

namespace Ida.Application.Features.MasterData.General;

public sealed class DoctorTypeSpec : SimpleMasterSpec<MstDoctorType>
{
    public override string Resource => "doctor-types";
    public override string DisplayNameTh => "ประเภทแพทย์";
    public override string Module => "master-data-general";
}

