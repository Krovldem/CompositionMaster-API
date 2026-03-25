using Microsoft.EntityFrameworkCore;
using CompositionMaster;
using CompositionMaster.Services;
using CompositionMaster.Services.Base;
using CompositionMaster.Services.Composition;
using CompositionMaster.Services.Dictionaries;
using CompositionMaster.Services.Helpers;
using CompositionMaster.Services.History;
using CompositionMaster.Services.Reports;
using CompositionMaster.Services.Search;
using DotNetEnv;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

// ===================== SERVICES ========================

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
    {
        policy.WithOrigins("http://localhost:5080", "https://localhost:5080")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".CompositionMaster.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor нужен UserHelper
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserHelper>();

// ===================== SESSION ========================

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CompositionMaster.Session";
});

// ===================== DATABASE ========================

var dbHost     = Environment.GetEnvironmentVariable("DB_HOST")     ?? "localhost";
var dbPort     = Environment.GetEnvironmentVariable("DB_PORT")     ?? "5432";
var dbName     = Environment.GetEnvironmentVariable("DB_NAME")     ?? "composition_db";
var dbUser     = Environment.GetEnvironmentVariable("DB_USER")     ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var dbSchema   = Environment.GetEnvironmentVariable("DB_SCHEMA")   ?? "public";

var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};" +
    $"Password={dbPassword};SearchPath={dbSchema}";

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseNpgsql(connectionString));

// ===================== РЕГИСТРАЦИЯ СЕРВИСОВ ========================

builder.Services.AddScoped<BaseService>();
builder.Services.AddScoped<DictionaryService>();
builder.Services.AddScoped<NomenclatureService>();
builder.Services.AddScoped<SpecificationService>();
builder.Services.AddScoped<OperationCardService>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<GenericService>();
builder.Services.AddScoped<DataSeedService>();

builder.Services.AddLogging();

var app = builder.Build();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
QuestPDF.Settings.License = LicenseType.Community;

// ===================== SWAGGER =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ================== DATABASE INIT =====================

using (var scope = app.Services.CreateScope())
{
    var context     = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!await context.Database.CanConnectAsync())
        {
            logger.LogWarning("База данных недоступна. Создаём новую...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("База данных создана. Загружаем seed-данные...");
            await seedService.SeedDataAsync();
        }
        else
        {
            logger.LogInformation("База данных существует.");
            var hasAnyData = await context.Roles.AnyAsync();

            if (!hasAnyData)
            {
                logger.LogInformation("Таблицы пустые. Загружаем seed-данные...");
                await seedService.SeedDataAsync();
            }
            else
            {
                logger.LogInformation("Проверяем изменения в seed-данных...");
                var hasChanges = await seedService.HasAnyChangesAsync();

                if (hasChanges)
                {
                    logger.LogInformation("Обнаружены изменения. Обновляем...");
                    await seedService.SeedDataAsync();
                    logger.LogInformation("Обновление завершено.");
                }
                else
                {
                    logger.LogInformation("Изменений нет.");
                }
            }
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            var rolesCount = await context.Roles.CountAsync();
            var usersCount = await context.Users.CountAsync();
            logger.LogDebug("Ролей: {Roles}, Пользователей: {Users}", rolesCount, usersCount);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при инициализации базы данных");
        if (app.Environment.IsDevelopment()) throw;
    }
}

// ===================== MIDDLEWARE ========================

app.UseHttpsRedirection();
app.UseSession();

// ---- Middleware: принимаем X-User-Id от CompositionUI и кладём в сессию ----
// Это позволяет UserHelper.GetCurrentUserId() работать корректно при запросах от фронта
app.Use(async (HttpContext context, Func<Task> next) =>
{
    var existingSession = context.Session.GetString("UserId");
    if (string.IsNullOrEmpty(existingSession))
    {
        var headerUserId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerUserId) && int.TryParse(headerUserId, out _))
        {
            context.Session.SetString("UserId", headerUserId);

            var headerUserRole = context.Request.Headers["X-User-Role"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerUserRole))
                context.Session.SetString("UserRole", headerUserRole);
        }
    }

    await next();
});

app.UseAuthorization();
app.UseCors("AllowWebClient");
app.UseAuthentication();
app.MapControllers();

app.Run();