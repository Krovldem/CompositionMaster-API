using System.Security.Claims;
using CompositionMaster.Models;
using CompositionMaster.Services.Composition;
using CompositionMaster.Services.Dictionaries;
using CompositionMaster.Services.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/composition")]
    public class CompositionController : ControllerBase
    {
        private readonly NomenclatureService _nomenclatureService;
        private readonly SpecificationService _specificationService;
        private readonly DictionaryService _dictionaryService;
        private readonly UserHelper _userHelper;

        public CompositionController(
            NomenclatureService nomenclatureService,
            SpecificationService specificationService,
            DictionaryService dictionaryService,
            UserHelper userHelper)
        {
            _nomenclatureService = nomenclatureService;
            _specificationService = specificationService;
            _dictionaryService = dictionaryService;
            _userHelper = userHelper;
        }

        // ===== РОЛИ =====
        [HttpGet("roles")]
        public ActionResult<List<Role>> GetRoles()
            => _dictionaryService.GetRoles();

        [HttpGet("roles/{id}")]
        public ActionResult<Role> GetRole(int id)
            => _dictionaryService.GetRole(id) is { } r ? Ok(r) : NotFound();

        // ===== ПОЛЬЗОВАТЕЛИ =====
        [HttpGet("users")]
        public ActionResult<List<User>> GetUsers()
            => _dictionaryService.GetUsersWithRoles();

        [HttpGet("users/{id}")]
        public ActionResult<User> GetUser(int id)
            => _dictionaryService.GetUserWithRole(id) is { } u ? Ok(u) : NotFound();

        [HttpGet("users/{id}/role")]
        public ActionResult<Role> GetUserRole(int id)
        {
            var user = _dictionaryService.GetUserWithRole(id);
            if (user == null) return NotFound();
            return user.Role != null ? Ok(user.Role) : NotFound();
        }

        [HttpPut("users/{id}/role")]
        public IActionResult SetUserRole(int id, [FromBody] int roleId)
        {
            var user = _dictionaryService.Get<User>(id);
            if (user == null) return NotFound();
            
            var role = _dictionaryService.GetRole(roleId);
            if (role == null) return NotFound();
            
            _dictionaryService.UpdateUserRole(id, roleId);
            return NoContent();
        }

        [HttpPost("users")]
        public IActionResult CreateUser(User user)
        {
            _dictionaryService.Create(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Identifier }, user);
        }

        [HttpPut("users/{id}")]
        public IActionResult UpdateUser(int id, User user)
        {
            if (id != user.Identifier) return BadRequest();
            
            var existing = _dictionaryService.Get<User>(id);
            if (existing == null) return NotFound();
            
            _dictionaryService.Update(user);
            return NoContent();
        }

        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(int id)
        {
            if (_specificationService.UserHasSpecifications(id))
                return BadRequest("Пользователь является владельцем спецификаций и не может быть удален");
                
            return _dictionaryService.Delete<User>(id) ? NoContent() : NotFound();
        }

        // ===== АУТЕНТИФИКАЦИЯ =====
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = _dictionaryService.GetUsersWithRoles()
                .FirstOrDefault(u => u.Login == request.Login && u.Password == request.Password);
    
            if (user == null)
                return Unauthorized(new { error = "Неверный логин или пароль" });
    
            // Сохраняем в сессию
            HttpContext.Session.SetString("UserId", user.Identifier.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserLogin", user.Login);
            HttpContext.Session.SetString("UserRole", user.Role?.Name ?? "Пользователь");
    
            // Также создаем куки аутентификации
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Identifier.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Пользователь")
            };
    
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };
    
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), authProperties);
    
            return Ok(new
            {
                user = new
                {
                    user.Identifier,
                    user.FullName,
                    user.Login,
                    Role = user.Role?.Name ?? "Пользователь"
                }
            });
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <summary>
/// Регистрация нового пользователя
/// </summary>
[HttpPost("auth/register")]
public IActionResult Register([FromBody] RegisterRequest request)
{
    try
    {
        if (request == null)
            return BadRequest(new { error = "Неверный формат запроса" });

        // Проверяем обязательные поля
        if (string.IsNullOrWhiteSpace(request.Login))
            return BadRequest(new { error = "Логин обязателен" });
        
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Пароль обязателен" });
        
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "ФИО обязательно" });

        // Проверяем, существует ли пользователь с таким логином
        var existingUser = _dictionaryService.GetUsersWithRoles()
            .FirstOrDefault(u => u.Login == request.Login);
        
        if (existingUser != null)
        {
            return BadRequest(new { error = "Пользователь с таким логином уже существует" });
        }

        // Проверяем, существует ли роль с ID=2
        var roleExists = _dictionaryService.GetRole(2) != null;
        var roleId = roleExists ? 2 : 1; // Если роль 2 не существует, используем роль 1

        // Создаем нового пользователя
        var newUser = new User
        {
            FullName = request.FullName.Trim(),
            Login = request.Login.Trim(),
            Password = request.Password,
            RoleId = roleId
        };

        // Сохраняем пользователя
        _dictionaryService.Create(newUser);

        // Загружаем созданного пользователя с ролью
        var createdUser = _dictionaryService.GetUserWithRole(newUser.Identifier);
        
        if (createdUser == null)
            return StatusCode(500, new { error = "Ошибка при загрузке созданного пользователя" });

        // Автоматически логиним пользователя
        HttpContext.Session.SetString("UserId", createdUser.Identifier.ToString());
        HttpContext.Session.SetString("UserName", createdUser.FullName);
        HttpContext.Session.SetString("UserLogin", createdUser.Login);
        HttpContext.Session.SetString("UserRole", createdUser.Role?.Name ?? "Инженер");

        return Ok(new
        {
            user = new
            {
                createdUser.Identifier,
                createdUser.FullName,
                createdUser.Login,
                Role = createdUser.Role?.Name ?? "Инженер"
            }
        });
    }
    catch (DbUpdateException ex)
    {
        // Логируем внутреннее исключение для отладки
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        return StatusCode(500, new { error = $"Ошибка базы данных: {innerMessage}" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = $"Ошибка при регистрации: {ex.Message}" });
    }
}

        [HttpPost("auth/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Выход выполнен" });
        }

        [HttpGet("auth/current-user")]
        public IActionResult GetCurrentUser()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Пользователь не авторизован" });
            
            return Ok(new
            {
                userId = int.Parse(userId),
                userName = userName,
                userRole = userRole
            });
        }

        // ===== НОМЕНКЛАТУРА =====
        [HttpGet("nomenclatures")]
        public ActionResult<List<Nomenclature>> GetNomenclatures()
            => _nomenclatureService.GetNomenclaturesWithDetails();

        [HttpGet("nomenclatures/{id}")]
        public ActionResult<Nomenclature> GetNomenclature(int id)
            => _nomenclatureService.GetNomenclatureWithDetails(id) is { } n ? Ok(n) : NotFound();

        [HttpPost("nomenclatures")]
        public IActionResult CreateNomenclature(Nomenclature nomenclature)
        {
            _nomenclatureService.CreateNomenclature(nomenclature, HttpContext);
            return CreatedAtAction(nameof(GetNomenclature), new { id = nomenclature.Identifier }, nomenclature);
        }

        [HttpPut("nomenclatures/{id}")]
        public IActionResult UpdateNomenclature(int id, Nomenclature nomenclature)
        {
            if (id != nomenclature.Identifier) return BadRequest();
            
            var oldVersion = _nomenclatureService.Get<Nomenclature>(id);
            if (oldVersion == null) return NotFound();
            
            _nomenclatureService.UpdateNomenclature(id, nomenclature, HttpContext);
            return NoContent();
        }

        [HttpDelete("nomenclatures/{id}")]
        public IActionResult DeleteNomenclature(int id)
        {
            if (_nomenclatureService.IsNomenclatureInUse(id))
                return BadRequest("Номенклатура используется в спецификациях и не может быть удалена");
            
            return _nomenclatureService.Delete<Nomenclature>(id) ? NoContent() : NotFound();
        }

        // ===== СПЕЦИФИКАЦИИ =====
        [HttpGet("specifications")]
        public ActionResult<List<Specification>> GetSpecifications()
            => _specificationService.GetSpecificationsWithOwner();

        [HttpGet("specifications/{id}")]
        public ActionResult<Specification> GetSpecification(int id)
            => _specificationService.Get<Specification>(id) is { } s ? Ok(s) : NotFound();

        // ===== КОМПОНЕНТЫ =====
        [HttpGet("specifications/{id}/components")]
        public ActionResult<List<SpecificationComponent>> GetComponents(int id)
            => _specificationService.GetSpecificationComponents(id);

        // ===== СПЕЦИАЛЬНЫЕ МЕТОДЫ =====
        [HttpGet("specifications/{id}/full")]
        public async Task<IActionResult> GetFull(int id)
            => await _specificationService.GetFullSpecificationAsync(id) is { } r ? Ok(r) : NotFound();

        [HttpGet("specifications/{id}/full-tree")]
        public async Task<IActionResult> GetFullTree(int id, [FromQuery] int maxLevel = 5)
            => await _specificationService.GetFullSpecificationTreeAsync(id, maxLevel) is { } r ? Ok(r) : NotFound();

        [HttpGet("specifications/{id}/flat")]
        public async Task<IActionResult> GetFlat(int id)
            => Ok(await _specificationService.GetFlatSpecificationAsync(id));

        [HttpGet("specifications/{id}/summary")]
        public async Task<IActionResult> GetSummary(int id)
            => await _specificationService.GetSpecificationSummaryAsync(id) is { } r ? Ok(r) : NotFound();
    }
    
    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}