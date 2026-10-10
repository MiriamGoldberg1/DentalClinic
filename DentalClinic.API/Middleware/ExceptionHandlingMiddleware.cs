using DentalClinic.API.Extensions;
using DentalClinic.Core.Exceptions;

namespace DentalClinic.API.Middleware;

/// <summary>טיפול גלובלי בשגיאות: כל חריגה ממופה לסטטוס HTTP ול-JSON אחיד; עסקית - Warning, לא צפויה - Error.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// ConflictException (כולל התנגשות Concurrency על יומן הרופא) מוחזרת כ-409 Conflict - לא 400 ולא 500,
    /// כי הבקשה תקינה בפני עצמה אבל מתנגשת במצב הנוכחי של המשאב. ההתנגשות נרשמת בלוג ברמת Warning.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.GetCorrelationId();
        using var scope = _logger.BeginScope(new Dictionary<string, object> { [CorrelationIdMiddleware.ItemKey] = correlationId });

        var (statusCode, message) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            BusinessRuleException => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, ex.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
            ConflictException => (StatusCodes.Status409Conflict, ex.Message),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (499, "הבקשה בוטלה על ידי הלקוח"),
            _ => (StatusCodes.Status500InternalServerError, "אירעה שגיאה לא צפויה בשרת. אנא נסה שוב מאוחר יותר.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else if (statusCode == StatusCodes.Status409Conflict)
        {
            _logger.LogWarning("Resource conflict on {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, ex.Message);
        }
        else if (statusCode == 499)
        {
            _logger.LogInformation("Request cancelled by client: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled {ExceptionType} on {Method} {Path}: {Message}",
                ex.GetType().Name, context.Request.Method, context.Request.Path, ex.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        await context.WriteErrorAsync(statusCode, message);
    }
}
