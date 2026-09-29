import { departmentScreen } from './general/department';
import { specialtyScreen } from './general/specialty';
import { subSpecialtyScreen } from './general/sub-specialty';
import { hospitalScreen } from './general/hospital';
import { clinicScreen } from './general/clinic';
import { doctorGroupScreen } from './general/doctor-group';
import { privilegeSubtypeScreen } from './general/privilege-subtype';
import { titleScreen } from './general/title';
import { doctorTypeScreen, privilegeTypeScreen, statusPrivilegeScreen, } from './general/simple';
import { arCodeScreen } from './accounting/ar-code';
import { bankScreen } from './accounting/bank';
import { bankBranchScreen } from './accounting/bank-branch';
import { glPostingSetupScreen } from './accounting/gl-posting-setup';
import { incomeDeductionItemScreen } from './accounting/income-deduction-item';
import { noWaitPaymentRuleScreen } from './accounting/no-wait-payment-rule';
import { paymentTypeScreen } from './accounting/payment-type';
import { receiptTypeScreen } from './accounting/receipt-type';
import { shareCategoryScreen } from './accounting/simple';
import { treatmentScreen } from './accounting/treatment';
import { treatmentCategoryScreen } from './accounting/treatment-category';
import { adjustmentTypeScreen } from './tax-402/adjustment-type';
import { expenseTypeScreen } from './tax-402/expense-type';
import { incomeType402Screen } from './tax-402/income-type-402';
import { pitTaxBracketScreen } from './tax-402/pit-tax-bracket';
import { taxAllowanceTypeScreen } from './tax-402/tax-allowance-type';
import { invoiceArCashRuleScreen } from './tax-406/invoice-ar-cash-rule';
import { invoicePrefixRuleScreen } from './tax-406/invoice-prefix-rule';
import { doctorScreen } from '../doctors/doctor';
import { doctorCodeScreen } from '../doctors/doctor-code';
import { doctorWelfareScreen, welfarePlanScreen } from '../doctors/welfare';
import { categoryShareScreen, categoryTreatmentShareScreen, doctorTreatmentDeptShareScreen, doctorTreatmentShareScreen, packageShareScreen, patientRightShareScreen, privateCaseShareScreen, treatmentShareScreen, } from '../share-rates/premium';
import { socialActivityShareScreen, socialArCodeShareScreen, socialBaseShareScreen, socialDepartmentTreatmentShareScreen, socialDoctorActivityShareScreen, socialDoctorTreatmentShareScreen, socialTreatmentShareScreen, } from '../share-rates/social';
import { dutyCheckinScreen, hourlyCheckinScreen, monthlyCheckinScreen, sessionCheckinScreen, } from '../duty-schedules/schedules';
import { positionFeeScreen } from '../doctor-fee-402/position-fee';
import { lumpSumUnitFeeScreen, outClinicFeeScreen } from '../doctor-fee-402/external-fee';
import { feeItemScreen } from '../doctor-fee-402/fee-item';
import { hospitalPaidTaxScreen, taxDeductionScreen, taxExemptionScreen, } from '../doctor-fee-402/tax';
import { badDebtTierScreen } from '../doctor-fee-406/bad-debt-tier';
import { slipSettingScreen } from '../income-documents/slip-setting';
import { userScreen } from '../users/user';
import { roleScreen } from '../users/role';
import { checkinAreaScreen, emailTemplateScreen, expiryAlertSettingScreen, hisDoctorCodeMapScreen, hisNotifyEmailScreen, incomeDocSettingScreen, passwordPolicyScreen, termsScreen, } from '../system-settings/settings';
import { holidayDutyRateScreen } from '../duty-rates/holiday';
import { dutyRateScreen } from '../duty-rates/duty';
import { hourlyGuaranteeScreen, lumpSumRateScreen, monthlyGuaranteeScreen, perSessionGuaranteeScreen, surplusRateScreen, } from '../duty-rates/screens';
import type { AnyScreenDescriptor } from './descriptor';
export const MASTER_DATA_SCREENS: readonly AnyScreenDescriptor[] = [
    specialtyScreen,
    subSpecialtyScreen,
    hospitalScreen,
    departmentScreen,
    clinicScreen,
    doctorTypeScreen,
    doctorGroupScreen,
    statusPrivilegeScreen,
    privilegeTypeScreen,
    privilegeSubtypeScreen,
    titleScreen,
    bankScreen,
    bankBranchScreen,
    incomeDeductionItemScreen,
    treatmentScreen,
    treatmentCategoryScreen,
    paymentTypeScreen,
    receiptTypeScreen,
    arCodeScreen,
    shareCategoryScreen,
    glPostingSetupScreen,
    noWaitPaymentRuleScreen,
    incomeType402Screen,
    adjustmentTypeScreen,
    expenseTypeScreen,
    pitTaxBracketScreen,
    taxAllowanceTypeScreen,
    invoicePrefixRuleScreen,
    invoiceArCashRuleScreen,
    doctorScreen,
    doctorCodeScreen,
    doctorWelfareScreen,
    welfarePlanScreen,
    privateCaseShareScreen,
    patientRightShareScreen,
    packageShareScreen,
    doctorTreatmentDeptShareScreen,
    doctorTreatmentShareScreen,
    categoryTreatmentShareScreen,
    treatmentShareScreen,
    categoryShareScreen,
    socialArCodeShareScreen,
    socialDoctorTreatmentShareScreen,
    socialDepartmentTreatmentShareScreen,
    socialDoctorActivityShareScreen,
    socialTreatmentShareScreen,
    socialActivityShareScreen,
    socialBaseShareScreen,
    holidayDutyRateScreen,
    dutyRateScreen,
    lumpSumRateScreen,
    surplusRateScreen,
    hourlyGuaranteeScreen,
    perSessionGuaranteeScreen,
    monthlyGuaranteeScreen,
    dutyCheckinScreen,
    hourlyCheckinScreen,
    sessionCheckinScreen,
    monthlyCheckinScreen,
    positionFeeScreen,
    lumpSumUnitFeeScreen,
    outClinicFeeScreen,
    feeItemScreen,
    hospitalPaidTaxScreen,
    taxDeductionScreen,
    taxExemptionScreen,
    badDebtTierScreen,
    slipSettingScreen,
    userScreen,
    roleScreen,
    emailTemplateScreen,
    hisNotifyEmailScreen,
    incomeDocSettingScreen,
    hisDoctorCodeMapScreen,
    expiryAlertSettingScreen,
    passwordPolicyScreen,
    checkinAreaScreen,
    termsScreen,
];
