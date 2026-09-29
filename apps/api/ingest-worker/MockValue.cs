using System.Text.Json.Nodes;

internal static class MockValue
{
    public static JsonNode? Create(Dataset dataset, string field, string hospital,
        DateOnly date, int revision, int receiptSeq)
    {
        var key = field.ToUpperInvariant();
        var type = dataset.PostmanTypes?.GetValueOrDefault(field);
        var amount = MockFixture.InvoiceAmount(revision);
        var receiptAmount = receiptSeq == 1 ? amount : 200m;
        var day = date.ToString("yyyy-MM-dd");

        if (key is "CXL_DATETIME" or "XRAY_CXL_DATE") return null;
        if (type == "boolean" || (type is null && key == "IS_ACTIVE"))
            return JsonValue.Create(key == "IS_ACTIVE");
        if (type == "number" || IsLegacyAmount(key))
            return JsonValue.Create(Number(key, dataset.Code, amount, receiptAmount, date));

        string value = key switch
        {
            "HOSPITAL_CODE" or "HOSPITALCODE" => hospital,
            "HOSPITAL_NO" or "HOSPITALNO" or "HN" or "HN_NO" => "MOCK-HN-0001",
            "HN_NAME" => "ผู้ป่วยทดสอบ",
            "TITLE_NAME" or "TITLENAME" => "นาย",
            "FIRST_NAME" or "FIRSTNAME_NAME" or "FIRSTNAME" => "ทดสอบ",
            "LAST_NAME" or "LASTNAME" => "ระบบ",
            "DOCTORNAME" or "DOCTOR_NAME" => "นพ. แพทย์ทดสอบ",
            "INVOICE_NO" or "INVOICENO" or "INVOICENOINSIDE" => $"MOCK-{hospital}-{date:yyyyMMdd}",
            "RECEIPTNO" => $"MOCK-REC-{hospital}-{date:yyyyMMdd}-{receiptSeq}",
            "RECEIVABLE_APPLICATION_ID" => $"MOCK-APP-{hospital}-{date:yyyyMMdd}-{receiptSeq}",
            "CASH_RECEIPT_ID" => $"MOCK-CASH-{hospital}-{date:yyyyMMdd}-{receiptSeq}",
            "VISIT_NO" or "VISITNO" or "VN_OPD" or "VISIT_OPD" => "MOCK-VN-0001",
            "REQUEST_NO" => "MOCK-REQ-0001",
            "AN" or "ADMIT_NO" or "ADMISSION_CODE" or "ADMISSIONTYPECODE" => "MOCK-AN-0001",
            "CLINIC_CODE" => "MOCK-CLINIC-01",
            "TREATMENT_CODE" => "MOCK-TREAT-01",
            "TREATMENT_CATEGORY_CODE" => "MOCK-CAT-01",
            "SPECIALTY_CODE" => "MOCK-SPEC-01",
            "SUB_SPECIALTY_CODE" => "MOCK-SUB-01",
            "ACTIVITY_CODE" => "MOCK-ACT-01",
            "DF_DOCTOR_CODE" or "DOCTOR_CODE" or "ORDER_DOCTOR_CODE" or
                "ORDER_DFDOCTOR_CODE" or "OLD_DOCTOR_CODE" => "MOCK-DR-01",
            "DF_DEPARTMENT_CODE" or "DEPARTMENT_CODE" or "ORDER_DEPARTMENT_CODE" or
                "ORDER_DFDEPARTMENT_CODE" or "OLD_DEPARTMENT_CODE" or
                "DEPARTMENT_BASED_EXPENSE_INCOME_CENTER_CODE" => "MOCK-DEPT-01",
            "DEPARTMENT_BASED_EXPENSE_INCOME_CENTER_TH" => "แผนกทดสอบ",
            "DEPARTMENT_BASED_EXPENSE_INCOME_CENTER_ENG" => "Mock Department",
            "CLINIC_NAME" or "CLINIC_NAME_TH" => "คลินิกทดสอบ",
            "CLINIC_NAME_ENG" => "Mock Clinic",
            "DESCRIPTION_TH" => dataset.Code switch
            {
                "his_clinic" => "คลินิกทดสอบ",
                "his_treatment" => "หัตถการทดสอบ",
                _ => "หมวดทดสอบ"
            },
            "DESCRIPTION_ENG" => dataset.Code switch
            {
                "his_clinic" => "Mock Clinic",
                "his_treatment" => "Mock Treatment",
                _ => "Mock Category"
            },
            "SPECIALTY_NAME_TH" or "SUB_SPECIALTY_NAME_TH" => "สาขาทดสอบ",
            "SPECIALTY_NAME_ENG" or "SUB_SPECIALTY_NAME_ENG" => "Mock Specialty",
            "LOCATION" or "LOCATION_CODE" or "LOCATION_CDOE" or
                "CASHIER_LOCATION_CODE" or "CASHIER_LOCATION_CDOE" or
                "CASHIERLOCATIONCODE" => "MOCK-OPD-01",
            "FACCODE" => "MOCK-FAC-01",
            "XRAY_CODE" => "MOCK-XRAY-01",
            "XRAY_STATUS" => "COMPLETE",
            "RIGHT_CODE" => "MOCK-RIGHT-01",
            "AR_CODE" => "MOCK-AR-01",
            "AR_PAYOR_NAME" => "ผู้ชำระเงินทดสอบ",
            "REFER_HOS_NAME" => "โรงพยาบาลทดสอบ",
            "RECEIPT_MODULE" or "RECRIPT_MODULE" or "RECEIPTMODULE" => "MOCK-AR",
            "RECEIPT_TYPE1" or "RECEIPTTYPECODE" => "CASH",
            "RECEIPT_TYPE2" or "RECEIPT_TYPE3" or "RECEIPT_TYPE4" or "RECEIPT_TYPE5" => "NONE",
            "DOCTYPE" => "R",
            "ISVOID" or "IS_PACKAGE" or "ISLOADED" or "SSOFLAG" or "SSOFLIG" or
                "PRIVATE_CASE" or "DF_CAL_FLAG" or "INTFLAG" => "N",
            "IS_VALUABLE_TO_DOCTORS" => "Y",
            "IS_MEDICAL_EQUIPMENT" => "N",
            "BATCH_NO" or "TRAN_BATCH_NO" or "RESULT_BATCH_NO" or "INTBATCHNO" =>
                $"MOCK-BATCH-{date:yyyyMMdd}",
            "TEL" => "020000000",
            "FAX" => "020000001",
            "REMARK" => "ข้อมูลจำลองสำหรับทดสอบ",
            "START" or "TIME" => "09:00",
            "END" or "TIME2" => "17:00",
            "TIME3" => "09:00",
            "TIME4" => "17:00",
            "INSPECTION_ROOM" => "MOCK-ROOM-01",
            "LAST_UPDATEUSER" => "MOCK-USER",
            "START_DATE" or "START_HOLIDAY" => day,
            "END_DATE" or "END_HOLIDAY" => date.AddYears(1).ToString("yyyy-MM-dd"),
            "INVOICE_DATE" or "INVOICEDATE" or "VISIT_DATE" or "VISITDATE" or
                "TRANSACTION_DATE" or "ACCRUAL_DATE" or "RECEIPTDATE" or
                "RESULT_DATE" or "MODIFY_DATE" or "ENTRY_DATE" or "LAST_UPDATEDATE" => day,
            "CHARGE_DATE_TIME" or "ORDER_DATE_TIME" => $"{day}T09:00:00",
            "MODIFY_TIME" => "09:00:00",
            "SSO_OPD_LIMIT" or "SSO_COPAY" => "0.00",
            "PAYMENT_BASE" or "PAYMENT_BASE_SOCIAL_IPD" or "PAYMENT_BASE_SOCIAL_OPD" => "100.00",
            "IS_ACTIVE" => "Y",
            _ when key.EndsWith("_NAME_TH") || key.EndsWith("_TH") => "ข้อมูลทดสอบ",
            _ when key.EndsWith("_NAME_ENG") || key.EndsWith("_ENG") => "Mock data",
            _ when key.EndsWith("_CODE") || key.EndsWith("CODE") => $"MOCK-{key}-01",
            _ when key.EndsWith("_NO") || key.EndsWith("NO") => $"MOCK-{key}-001",
            _ when key.Contains("DATE") => day,
            _ => $"MOCK-{key}"
        };
        return JsonValue.Create(value);
    }

    private static bool IsLegacyAmount(string key) => key.Contains("AMOUNT") ||
        key.Contains("AMT") || key is "NONEDF_AMOUNT" or "ORIGINAL_CHARGE_AMOUNT";

    private static decimal Number(string key, string dataset, decimal amount,
        decimal receiptAmount, DateOnly date)
    {
        if (key.Contains("DISCOUNT") && (key.Contains("_OF_") || key.Contains("OF"))) return 0m;
        if (key is "NONEDF_AMOUNT") return 0m;
        if (key.StartsWith("RECEIPT_AMOUNT", StringComparison.Ordinal) && key != "RECEIPT_AMOUNT1") return 0m;
        if (key.Contains("AMOUNT") || key.Contains("AMT"))
            return dataset == "oracle_ar" || key.StartsWith("REC_") || key.StartsWith("RECEIPT_")
                ? receiptAmount : dataset is "his_none_df" or "his_accrual_no_invoice" ? 0m : amount;
        return key switch
        {
            "DAY_OF_WEEK" => ((int)date.DayOfWeek + 6) % 7 + 1,
            "LIMIT" => 20m,
            "WAIT_LIMIT" => 5m,
            "NOMINUTES_OPDDIAGTIME" or "NOMINUTES_EACHINSERT" => 15m,
            "I_ENABLED" => 1m,
            "CALCULATE_TYPE" => 1m,
            "SUFFIX_SMALL" or "INVOICE_SUFFIX_SMALL" => 1m,
            _ => 0m
        };
    }
}

