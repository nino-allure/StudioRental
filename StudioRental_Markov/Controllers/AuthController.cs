using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    /// <summary>
    /// Контроллер для управления аутентификацией и регистрацией пользователей.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtService _jwtService;
        private readonly LoggingService _logging;

        public AuthController(AppDbContext db, JwtService jwtService, LoggingService logging)
        {
            _db = db;
            _jwtService = jwtService;
            _logging = logging;
        }

        /// <summary>
        /// Регистрация нового пользователя в системе.
        /// </summary>
        /// <param name="request">Данные для регистрации (Email, Password, FullName, Phone).</param>
        /// <returns>JWT токен и данные пользователя при успешной регистрации.</returns>
        /// <response code="200">Пользователь успешно зарегистрирован.</response>
        /// <response code="400">Ошибка валидации данных или Email уже занят.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await _logging.LogAuthAsync("Register", $"Ошибка валидации при регистрации: {errors}", false);
                return BadRequest(new { message = errors });
            }

            await _logging.LogAuthAsync("Register", $"Попытка регистрации для email: {request.Email}", false);

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                await _logging.LogAuthAsync("Register", $"Ошибка регистрации: email {request.Email} уже используется", false);
                return BadRequest(new { message = "Этот email уже зарегистрирован" });
            }

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone ?? "",
                Role = "User",
                CreatedAt = DateTime.Now,
                Password = request.Password
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);
            await _logging.LogAuthAsync("Register", $"Пользователь {request.Email} успешно зарегистрирован", true, $"UserId: {user.Id}");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id,
                Email = user.Email
            });
        }

        /// <summary>
        /// Вход в систему (аутентификация) по Email и паролю.
        /// </summary>
        /// <param name="request">Учетные данные пользователя (Email, Password).</param>
        /// <returns>JWT токен и данные пользователя.</returns>
        /// <response code="200">Успешный вход в систему.</response>
        /// <response code="400">Ошибка валидации данных.</response>
        /// <response code="401">Неверный email или пароль.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { message = errors });
            }

            await _logging.LogAuthAsync("Login", $"Попытка входа для email: {request.Email}", false);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.Password != request.Password)
            {
                await _logging.LogAuthAsync("Login", $"Неудачная попытка входа: {request.Email}", false);
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var token = _jwtService.GenerateToken(user);
            await _logging.LogAuthAsync("Login", $"Пользователь {request.Email} успешно вошел в систему", true, $"UserId: {user.Id}");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                UserId = user.Id,
                Email = user.Email
            });
        }
    }
}