using AutoMapper;
using DentalClinic.Core.DTOs.Auth;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Enums;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService,
        IMapper mapper, IDateTimeProvider clock, ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>הרשמת מטופל חדש: יוצר רשומת מטופל ומשתמש בתפקיד Patient, ומחזיר JWT.</summary>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, ct))
        {
            throw new ConflictException("כתובת האימייל כבר רשומה במערכת");
        }

        if (dto.DateOfBirth >= _clock.Today || dto.DateOfBirth.Year < 1900)
        {
            throw new BusinessRuleException("תאריך הלידה אינו תקין");
        }

        var patient = _mapper.Map<PatientRequestDto, Patient>(dto);
        _mapper.Map(dto.Address, patient.Address);

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.Patient,
            Patient = patient,
            CreatedAtUtc = _clock.UtcNow
        };

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(ct);

        // לא כותבים ללוג את הסיסמה או את תוכן בקשת ההרשמה - רק מזהים
        _logger.LogInformation("New patient registered. UserId {UserId}, PatientId {PatientId}", user.Id, patient.Id);
        user.PatientId = patient.Id;
        return BuildResponse(user);
    }

    /// <summary>התחברות: אימות סיסמה מול הגיבוב השמור והחזרת JWT. שגיאה כללית אחת לאימייל או סיסמה שגויים.</summary>
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(dto.Email, ct);
        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt");
            throw new UnauthorizedException("אימייל או סיסמה שגויים");
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Email = user.Email,
            Role = user.Role.ToString(),
            PatientId = user.PatientId
        };
    }
}
