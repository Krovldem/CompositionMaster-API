using System.Security.Claims;
using CompositionMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.Helpers;

public class UserHelper
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ApplicationContext _context;
    private readonly ILogger<UserHelper> _logger;

    public UserHelper(
        IHttpContextAccessor? httpContextAccessor = null,
        ApplicationContext context = null,
        ILogger<UserHelper> logger = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Возвращает ID текущего пользователя.
    /// Порядок поиска:
    ///   1. Сессия API-сервера
    ///   2. Заголовок X-User-Id (проброс от MVC-фронта через SessionCookieHandler)
    ///   3. Claims (cookie-аутентификация)
    ///   4. Fallback = 1 (для разработки)
    /// </summary>
    public int GetCurrentUserId(HttpContext? httpContext = null)
    {
        var context = httpContext ?? _httpContextAccessor?.HttpContext;
        if (context == null) return 1;

        // 1. Из собственной сессии API
        var userIdSession = context.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdSession) &&
            int.TryParse(userIdSession, out int userIdFromSession))
        {
            return userIdFromSession;
        }

        // 2. Из заголовка X-User-Id (проброс от CompositionUI через SessionCookieHandler)
        var headerUserId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerUserId) &&
            int.TryParse(headerUserId, out int userIdFromHeader))
        {
            _logger?.LogDebug("GetCurrentUserId: из заголовка X-User-Id = {UserId}", userIdFromHeader);
            return userIdFromHeader;
        }

        // 3. Из Claims (cookie-аутентификация)
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null &&
            int.TryParse(userIdClaim.Value, out int userIdFromClaim))
        {
            return userIdFromClaim;
        }

        _logger?.LogDebug("GetCurrentUserId: пользователь не определён, fallback = 1");
        return 1;
    }

    public string GetChangesDescription<T>(T oldObj, T newObj)
    {
        var changes = new System.Text.StringBuilder();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (prop.Name == "Identifier" || prop.Name == "ChangeDate" || prop.Name == "Author")
                continue;

            var oldValue = prop.GetValue(oldObj);
            var newValue = prop.GetValue(newObj);

            if (!Equals(oldValue, newValue))
            {
                if (changes.Length > 0) changes.Append("; ");
                changes.Append($"{prop.Name}: '{oldValue}' -> '{newValue}'");
            }
        }

        return changes.Length > 0 ? changes.ToString() : "Без изменений";
    }

    public async Task<User?> AuthenticateUserAsync(string login, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                return null;

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user != null && user.Password == password)
            {
                _logger?.LogInformation("Пользователь {Login} аутентифицирован", login);
                return user;
            }

            _logger?.LogWarning("Неудачная попытка аутентификации для {Login}", login);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при аутентификации {Login}", login);
            return null;
        }
    }

    public async Task<User?> RegisterUserAsync(string login, string password, string fullName)
    {
        try
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(fullName))
            {
                _logger?.LogWarning("Попытка регистрации с пустыми данными");
                return null;
            }

            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login);

            if (existing != null)
            {
                _logger?.LogWarning("Пользователь {Login} уже существует", login);
                return null;
            }

            var roleExists = await _context.Roles.AnyAsync(r => r.Identifier == 2);
            var roleId = roleExists ? 2 : 1;

            var user = new User
            {
                Login = login.Trim(),
                Password = password,
                FullName = fullName.Trim(),
                RoleId = roleId
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var createdUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Identifier == user.Identifier);

            _logger?.LogInformation("Зарегистрирован {Login} ID={UserId}", login, user.Identifier);
            return createdUser;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при регистрации {Login}", login);
            return null;
        }
    }

    public List<User> GetAllUsers()
    {
        try
        {
            return _context.Users.Include(u => u.Role).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при получении пользователей");
            return new List<User>();
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Identifier == id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при получении пользователя {Id}", id);
            return null;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, int roleId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return false;

            user.RoleId = roleId;
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Роль {UserId} → {RoleId}", userId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при смене роли {UserId}", userId);
            return false;
        }
    }
}