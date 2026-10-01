namespace Ida.Domain.Common;

public enum RecordStatus
{
    Active,
    Inactive,
}

public enum ApprovalStatus
{
    Draft,
    Pending,
    Approved,
    Returned,
    Rejected,
    Cancelled,
}

public enum GenderType
{
    M,
    F,
    U,
}

public enum EmploymentStatus
{
    Working,
    Resigned,
    Suspended,
}

public enum WelfareScope
{
    None,
    DoctorOnly,
    DoctorAndFamily,
}

public enum RelationGroup
{
    Family,
    Relative,
    Reference,
}

public enum IdDocType
{
    NationalId,
    Passport,
}

public enum TaxEntityType
{
    Individual,
    Juristic,
}

public enum ItemDirection
{
    Add,
    Deduct,
}

public enum ReceiptPaymentForm
{
    Ar,
    Cash,
    Cheque,
    CreditCard,
    Dp,
    Dpc,
    Invoice,
}

public enum GlPostingDateRule
{
    BatchDate,
    MonthEnd,
    PaymentDate,
}

public enum InvoiceCalcMode
{

    NormalShare,

    ToHospital,
}

public enum AdmissionType
{
    All,
    Ipd,
    Opd,
}

public enum ShareRateLevel
{

    DoctorTreatmentDepartment,

    DoctorTreatment,

    PatientRightArCode,

    PrivateCase,

    Package,

    CategoryTreatment,

    Treatment,

    Category,

    SocialArCode,

    SocialDoctorTreatment,

    SocialDepartmentTreatment,

    SocialDoctorActivity,

    SocialTreatment,

    SocialActivity,

    SocialBase,
}

public enum ShareRateScheme
{
    Premium,
    SocialSecurity,
}

public enum SocialKind
{

    Pure,

    Shared,
}

public enum ShareTaxKind
{

    Tax406,

    NoTaxBase,
}

public enum ShareTaxBase
{
    BeforeShare,
    AfterShare,
}

public enum ShareMode
{
    Percent,
    FixAmount,
}

public enum HolidayPayMode
{

    Multiplier,

    Hourly,
}

public enum DutyPayKind
{
    Normal,
    OnTop,
    LumpSum,
    Surplus,
}

public enum ExternalFeeKind
{

    LumpSumUnit,

    OutClinic,
}

public enum FeeItemType
{

    MeetingAllowance,

    SpeakerPromotion,

    SpeakerPromotionEvent,

    SpeakerTraining,

    AnnualCompensation,

    PerCaseCompensation,

    HealthCheckConsultant,

    TravelCompensation,
}

public enum DutyScheduleKind
{

    Duty,

    GuaranteeHourly,

    GuaranteeSession,

    GuaranteeMonthly,
}

public enum DutyScheduleStatus
{

    Draft,

    Submitted,

    Calculated,

    Rejected,
}

public enum DutyRoom
{
    Room1,
    Room2,
    Room3,
    Room4,
    Room5,
    Other,
}

public enum GuaranteeKind
{

    LumpSum,

    Surplus,

    Hourly,

    PerSession,

    Monthly,
}

public enum GuaranteeCalcMode
{

    Normal,

    Cumulative,
}

public enum WorkTimeRule
{

    ByWorkTime,

    IgnoreTime,
}

public enum IncomeBase
{
    BeforeShare,
    AfterShare,
}

public enum GuaranteeBasis
{
    AccrualBasic,

    AccrualNoWaitPayment,
    CashBasic,

    CashAllReceiptsInMonth,
}

public enum CreditCardFeeBase
{
    BeforeFee,
    AfterFee,
}

public enum TreatmentScopeKind
{
    Include,
    Exclude,
}

public enum DocumentSendCycle
{
    Monthly,
    Yearly,
}

public enum PayslipArDetail
{

    Hide,

    CurrentMonth,

    All,
}

