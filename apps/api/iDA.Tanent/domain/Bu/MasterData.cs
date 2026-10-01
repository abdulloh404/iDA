using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class MstDoctorType : TenantMasterEntity;

public class MstDoctorGroup : TenantMasterEntity
{
    public Guid? DoctorTypeId { get; set; }
    public MstDoctorType? DoctorType { get; set; }
}

public class MstDepartment : TenantMasterEntity
{
    public string? CostCenter { get; set; }
}

public class MstClinic : TenantMasterEntity
{
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
}

public class MstPaymentType : TenantMasterEntity
{

    public bool RequireBankAccount { get; set; }
}

public class MstPrivilegeType : TenantMasterEntity;

public class MstPrivilegeSubtype : TenantMasterEntity
{
    public Guid PrivilegeTypeId { get; set; }
    public MstPrivilegeType? PrivilegeType { get; set; }
}

public class MstStatusPrivilege : TenantMasterEntity;

public class MstWelfarePlan : TenantMasterEntity
{
    public WelfareScope WelfareScope { get; set; } = WelfareScope.None;
    public decimal AnnualLimit { get; set; }
}

