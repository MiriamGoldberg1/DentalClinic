# מרפאת שיניים - מערכת לזימון תורים (שרת)

שרת **ASP.NET Core Web API (.NET 6, C#)** בארכיטקטורה נקייה לזימון תורים במרפאת שיניים.
המערכת מנהלת מטופלים, רופאים ותורים, עם הרשמה והתחברות ב-JWT ושני תפקידים: מנהל מרפאה (Admin) ומטופל (Patient).
תורים נקבעים רק בימים א'-ה' בין 11:00 ל-18:00, בחלונות של 30 דקות. בסיס הנתונים הוא PostgreSQL.
הלקוח (React) נמצא ב-repository נפרד: `dental-clinic-client`.

## המשאב המוגבל והטיפול בתחרות

המשאב המוגבל הוא **שעה ביומן של רופא**: בכל חלון של 30 דקות רופא מקבל מטופל אחד בלבד.
התחרות קורית כששני מטופלים מנסים לקבוע תור אצל אותו רופא באותו רגע. הפתרון הוא **Optimistic Concurrency**:
לרופא יש Concurrency Token (`Version`), וכל קביעת תור מחליפה אותו באותה טרנזקציה שבה נשמר התור.
הבקשה שמגיעה שנייה נכשלת ב-`DbUpdateConcurrencyException`, מתבצע ניסיון חוזר עם נתונים טריים,
ואם השעה כבר נתפסה המשתמש מקבל **409 Conflict** עם הודעה ברורה. כל התנגשות נרשמת בלוג ברמת Warning.
פירוט מלא: `CONCURRENCY_HE.txt`. הסבר על הלוגים: `NLOG_HE.txt`.

## מבנה ה-Solution

| פרויקט | תוכן | מפנה אל |
|---|---|---|
| `DentalClinic.Core` | ישויות, DTOs, Enums, ממשקים, חריגות | - |
| `DentalClinic.Data` | DbContext, Fluent API, Repositories, Migrations, Seed | Core |
| `DentalClinic.Service` | לוגיקה עסקית, AutoMapper, JWT, גיבוב סיסמאות | Core |
| `DentalClinic.API` | Controllers, Middleware, Program.cs, NLog | Core, Service (Data לרישום בלבד) |
| `DentalClinic.Tests` | xUnit + Moq + בדיקת Concurrency | Core, Data, Service |

בכל פרויקט יש קובץ `LAYER_INFO.txt` שמסביר את תפקיד השכבה.

## דרישות מקדימות

1. **.NET 6 SDK** (או SDK חדש יותר עם .NET 6 Runtime): https://dotnet.microsoft.com/download/dotnet/6.0
2. **Visual Studio 2022** (17.0 ומעלה).
3. **PostgreSQL** (גרסה 13 ומעלה) שרץ מקומית על פורט 5432, עם המשתמש `postgres`.
   אין צורך ליצור את בסיס הנתונים - השרת יוצר אותו ומריץ את ה-migrations בעלייה.
4. כלי EF (רק אם רוצים ליצור migrations חדשות): `dotnet tool install --global dotnet-ef --version 6.0.36`

## הרצה מקומית

```bash
# 1. מתיקיית ה-Solution: שמירת הסודות ב-User Secrets (סיסמת PostgreSQL + מפתח JWT אקראי)
powershell -ExecutionPolicy Bypass -File setup-secrets.ps1      # Windows
./setup-secrets.sh                                              # Linux / Mac

# 2. הרצת השרת
dotnet run --project DentalClinic.API
```

או ידנית:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=DentalClinicDb;Username=postgres;Password=YOUR_PASSWORD" --project DentalClinic.API
dotnet user-secrets set "Jwt:Key" "any-random-string-of-at-least-32-characters" --project DentalClinic.API
```

ב-Visual Studio: פותחים את `DentalClinic.sln`, מגדירים את `DentalClinic.API` כ-Startup Project ולוחצים F5.
(אפשר גם לנהל את הסודות דרך קליק ימני על DentalClinic.API ← Manage User Secrets.)

- Swagger: http://localhost:5001/swagger
- Health check: http://localhost:5001/health

בעלייה הראשונה השרת מריץ את שתי ה-migrations ומכניס נתוני דמו (3 רופאים, 3 מטופלים, תורים לדוגמה) - ללא התערבות ידנית.

## משתמשי דמו

| תפקיד | אימייל | סיסמה |
|---|---|---|
| Admin (מנהל מרפאה) | admin@clinic.local | Admin123! |
| Patient (מטופל - אברהם ישראלי) | patient@clinic.local | Patient123! |

אפשר גם להירשם כמטופל חדש דרך `POST /api/auth/register`.

## Endpoints עיקריים

| Method | נתיב | הרשאה |
|---|---|---|
| POST | /api/auth/register, /api/auth/login | פתוח |
| GET | /api/patients?page=&pageSize=&search=&sortBy=&sortDesc= | Admin |
| GET / PUT | /api/patients/{id} | Admin או המטופל עצמו |
| POST | /api/patients | Admin |
| GET | /api/patients/{id}/appointments | Admin או המטופל עצמו |
| GET | /api/doctors, /api/doctors/{id}, /api/doctors/{id}/available-slots?date= | משתמש מחובר |
| POST / PUT | /api/doctors, /api/doctors/{id} | Admin |
| GET | /api/appointments (מטופל רואה רק את שלו), /api/appointments/{id} | משתמש מחובר |
| POST | /api/appointments | Admin / Patient |
| DELETE | /api/appointments/{id} | Admin או בעל התור |
| GET | /api/specializations | פתוח |

## בדיקות

```bash
dotnet test
```

`DoctorScheduleConcurrencyTests` היא ההוכחה לטיפול בתחרות: שני DbContext נפרדים קוראים את אותו רופא,
שניהם משנים, הראשון שומר והשני מקבל `DbUpdateConcurrencyException`. היא רצה על SQLite בזיכרון ולא דורשת PostgreSQL.

## Migrations

שתי migrations נמצאות ב-`DentalClinic.Data/Migrations` (`InitialCreate`, `AddUsersAndSpecializations`).
יצירת migration חדשה (משורש ה-Solution):

```bash
dotnet ef migrations add <Name> -p DentalClinic.Data -s DentalClinic.API
dotnet ef database update -p DentalClinic.Data -s DentalClinic.API
```

## מוכנות לענן

- כל ההגדרות נטענות מהקונפיגורציה, ובענן נקבעות במשתני סביבה: `ConnectionStrings__Default`, `Jwt__Key`, `Cors__AllowedOrigins__0`.
- השרת Stateless (JWT), ולכן אפשר להריץ כמה מופעים במקביל; ה-Concurrency Token מגן על היומן גם בין מופעים.
- `EnableRetryOnFailure` לתקלות רשת רגעיות מול בסיס הנתונים, `/health` לבדיקת חיות, Forwarded Headers מאחורי Load Balancer.
- כל התאריכים נשמרים ב-UTC; "היום" מחושב לפי `Clinic:TimeZoneId` (שעון השרת בענן הוא בדרך כלל UTC).
- לוגים לקונסול (בנוסף לקבצים) לאיסוף מרכזי.
- עם כמה מופעים מומלץ להגדיר `Database__MigrateOnStartup=false` ולהריץ migrations כשלב נפרד בפריסה.
