namespace Ida.Application.Common;

public class ApiException(int status, string code, string message,
    object? details = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public object? Details { get; } = details;

    public static ApiException BadRequest(string code, string message,
        object? details = null) => new(400, code, message, details);

    public static ApiException NotFound(string code, string message) =>
        new(404, code, message);

    public static ApiException Unauthorized(string code = "unauthorized",
        string message = "กรุณาเข้าสู่ระบบใหม่") => new(401, code, message);

    public static ApiException Forbidden(string code = "forbidden",
        string message = "คุณไม่มีสิทธิ์ใช้งานเมนูนี้") => new(403, code, message);

    public static ApiException Conflict(string code, string message,
        object? details = null) => new(409, code, message, details);

    public static ApiException DuplicateCode(string field, string message) =>
        Conflict("duplicate_code", message, new ValidationDetails(
            [new FieldError(field, "duplicate", message)]));

    public static ApiException ConcurrencyConflict() => Conflict(
        "concurrency_conflict",
        "ข้อมูลถูกแก้ไขโดยผู้ใช้อื่น กรุณาโหลดข้อมูลใหม่แล้วบันทึกอีกครั้ง");

    public static ApiException EntityInUse(string message) =>
        Conflict("entity_in_use", message);
}

