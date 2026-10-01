namespace Ida.Application.Common;

public record FieldError(string Field, string Code, string Message);

public record ValidationDetails(IReadOnlyList<FieldError> Fields);

public class ValidationFailure
{
    private readonly List<FieldError> _fields = [];

    public bool HasErrors => _fields.Count > 0;

    public ValidationFailure Add(string field, string code, string message)
    {
        _fields.Add(new FieldError(field, code, message));
        return this;
    }

    public ValidationFailure Required(string field, string message) =>
        Add(field, "required", message);

    public ValidationFailure Duplicate(string field, string message) =>
        Add(field, "duplicate", message);

    public void ThrowIfInvalid()
    {
        if (!HasErrors) return;

        throw new ApiException(422, "validation_failed",
            "ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบรายการที่ระบุ",
            new ValidationDetails(_fields));
    }
}

