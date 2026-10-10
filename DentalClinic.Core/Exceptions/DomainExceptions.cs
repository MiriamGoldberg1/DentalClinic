namespace DentalClinic.Core.Exceptions;

// חריגות עסקיות. שכבת ה-API ממפה כל אחת לסטטוס HTTP מתאים ב-Middleware.

/// <summary>הישות המבוקשת לא נמצאה (404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>הפרה של כלל עסקי, למשל תור ביום שישי (400).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>אין הרשאה לפעולה על משאב זה (403).</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>פרטי התחברות שגויים (401).</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>התנגשות עם המצב הנוכחי של המשאב, למשל תור שכבר נתפס (409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// התנגשות Optimistic Concurrency: הנתון שונה ע"י משתמש אחר בין הקריאה לשמירה.
/// נזרקת משכבת ה-Data במקום DbUpdateConcurrencyException, כדי ששכבת ה-Service לא תכיר את EF.
/// </summary>
public class ConcurrencyConflictException : ConflictException
{
    public ConcurrencyConflictException(string message, Exception? inner = null) : base(message, inner) { }
}
