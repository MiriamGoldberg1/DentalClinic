using System.Security.Claims;
using DentalClinic.Core.Common;

namespace DentalClinic.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>ממיר את ה-claims של ה-JWT לאובייקט CurrentUser שמועבר לשכבת ה-Service.</summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var patientIdValue = principal.FindFirst(CustomClaims.PatientId)?.Value;

        var userId = int.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : 0;
        int? patientId = int.TryParse(patientIdValue, out var parsedPatientId) ? parsedPatientId : null;

        return new CurrentUser(userId, role, patientId);
    }
}
//CurrentUser המרת הנתונים שהתקבלו לאובייקט 