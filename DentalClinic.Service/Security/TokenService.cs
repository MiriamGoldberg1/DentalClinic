using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DentalClinic.Core.Common;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Core.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DentalClinic.Service.Security;

/// <summary>יוצר JWT חתום עם claims: מזהה משתמש, אימייל, תפקיד ומזהה מטופל (אם יש).</summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public TokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    /// <summary>בונה וחותם את ה-token (HMAC-SHA256) לפי ההגדרות מהקונפיגורציה.</summary>
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.PatientId.HasValue)
        {
            claims.Add(new Claim(CustomClaims.PatientId, user.PatientId.Value.ToString()));
        }

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
