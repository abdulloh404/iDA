using Ida.Application.Common;

namespace Ida.Api;

public class ErrorEnvelopeMiddleware(RequestDelegate next,
    ILogger<ErrorEnvelopeMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);

            if (!ctx.Response.HasStarted && ctx.Response.ContentLength is null or 0)
            {
                var envelope = ctx.Response.StatusCode switch
                {
                    401 => ("unauthorized", "กรุณาเข้าสู่ระบบใหม่"),
                    403 => ("forbidden", "คุณไม่มีสิทธิ์ใช้งานเมนูนี้"),
                    400 => ("malformed_request", "รูปแบบข้อมูลที่ส่งมาไม่ถูกต้อง"),
                    405 => ("method_not_allowed", "ไม่รองรับการเรียกใช้รูปแบบนี้"),
                    _ => default,
                };

                if (envelope != default)
                    await WriteAsync(ctx, ctx.Response.StatusCode, envelope.Item1, envelope.Item2);
            }
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            log.LogDebug("Request canceled: {Method} {Path} ({TraceId})", ctx.Request.Method, ctx.Request.Path, ctx.TraceIdentifier);
            if (!ctx.Response.HasStarted)
                ctx.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (ApiException ex)
        {
            await WriteAsync(ctx, ex.Status, ex.Code, ex.Message, ex.Details);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled error: {Method} {Path} ({TraceId})", ctx.Request.Method, ctx.Request.Path, ctx.TraceIdentifier);
            await WriteAsync(ctx, 500, "internal_error", "เกิดข้อผิดพลาดภายในระบบ");
        }
    }

    private static Task WriteAsync(HttpContext ctx, int status, string code, string message,
        object? details = null)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                details,

                traceId = ctx.TraceIdentifier,
            },
        });
    }
}
