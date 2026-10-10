using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using DentalClinic.API.Extensions;
using DentalClinic.API.Json;
using DentalClinic.API.Middleware;
using DentalClinic.API.Models;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Core.Settings;
using DentalClinic.Data;
using DentalClinic.Data.Repositories;
using DentalClinic.Data.Seed;
using DentalClinic.Service.Infrastructure;
using DentalClinic.Service.Mapping;
using DentalClinic.Service.Security;
using DentalClinic.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

// NLog נטען ראשון, כדי שגם שגיאות בעליית השרת יירשמו ללוג
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------- Logging: ILogger של .NET בקוד, NLog מתחת ----------
    LogManager.Configuration.Variables["minLevel"] = builder.Configuration["LogSettings:MinLevel"] ?? "Info";
    LogManager.ReconfigExistingLoggers();
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ---------- Configuration (IOptions) ----------
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<ClinicSettings>(builder.Configuration.GetSection(ClinicSettings.SectionName));

    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
    if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key is missing (or shorter than 32 chars). Set it with: " +
            "dotnet user-secrets set \"Jwt:Key\" \"<random key of 32+ chars>\" --project DentalClinic.API");
    }

    var connectionString = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Default is missing. Set it with: " +
            "dotnet user-secrets set \"ConnectionStrings:Default\" \"Host=localhost;Port=5432;Database=DentalClinicDb;Username=postgres;Password=Miri1\" --project DentalClinic.API");
    }

    // ---------- Data: DbContext (Scoped) מול PostgreSQL ----------
    // EnableRetryOnFailure - ניסיון חוזר בתקלות רשת רגעיות, חשוב במיוחד בענן
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

    builder.Services.AddScoped<IPatientRepository, PatientRepository>();
    builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
    builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
    builder.Services.AddScoped<ISpecializationRepository, SpecializationRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<DbInitializer>();

    // ---------- Service: לוגיקה עסקית (Scoped - כמו ה-DbContext שהם תלויים בו) ----------
    builder.Services.AddScoped<IPatientService, PatientService>();
    builder.Services.AddScoped<IDoctorService, DoctorService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<ISpecializationService, SpecializationService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // שירותים ללא מצב וללא תלות ב-DbContext - Singleton
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddSingleton<ITokenService, TokenService>();

    builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

    // ---------- Controllers, JSON ו-ModelState ----------
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            // ModelState לא תקין (Data Annotations) => 400 במבנה השגיאה האחיד
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "ערך לא תקין" : e.ErrorMessage).ToArray());

                var body = new ErrorResponse(StatusCodes.Status400BadRequest, "הנתונים שנשלחו אינם תקינים",
                    context.HttpContext.GetCorrelationId(), errors);
                return new BadRequestObjectResult(body);
            };
        });

    // ---------- Swagger ----------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dental Clinic API", Version = "v1", Description = "מערכת לזימון תורים למרפאת שיניים" });
        options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date", Example = new OpenApiString("2026-10-11") });
        options.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("11:30") });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "הדבק כאן את ה-token שהתקבל מ-/api/auth/login (בלי המילה Bearer)"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });

        var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
        if (File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }
    });

    // ---------- JWT Authentication & Authorization ----------
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            // 401 / 403 גם הם במבנה השגיאה האחיד
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await context.HttpContext.WriteErrorAsync(StatusCodes.Status401Unauthorized, "נדרשת התחברות למערכת");
                },
                OnForbidden = context => context.HttpContext.WriteErrorAsync(StatusCodes.Status403Forbidden, "אין לך הרשאה לפעולה זו")
            };
        });
    builder.Services.AddAuthorization();

    // ---------- CORS ללקוח React (הכתובות נקבעות בקונפיגורציה) ----------
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

    // ---------- מוכנות לענן ----------
    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });


    var app = builder.Build();

    // ---------- הקמת בסיס הנתונים: migrations + seed, ללא התערבות ידנית ----------
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync(app.Configuration.GetValue("Database:MigrateOnStartup", true), app.Lifetime.ApplicationStopping);
    }

    // ---------- Pipeline: טיפול בשגיאות ראשון, Authentication לפני Authorization ----------
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseForwardedHeaders();

    if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dental Clinic API v1"));
    }

    app.UseCors("Client");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "StopTheHostException" and not "HostAbortedException")
{
    // שגיאה בעליית השרת (למשל סוד חסר או בסיס נתונים לא זמין)
    logger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

// מאפשר גישה למחלקת Program (למשל לבדיקות אינטגרציה)
public partial class Program
{
}




