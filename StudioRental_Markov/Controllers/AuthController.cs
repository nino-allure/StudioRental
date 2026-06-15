using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
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
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            await _logging.LogAuthAsync("Register", $"Попытка регистрации для email: {request.Email}", false);

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.FullName))
            {
                await _logging.LogAuthAsync("Register", $"Ошибка регистрации: не все поля заполнены для {request.Email}", false);
                return BadRequest(new { message = "Все поля обязательны" });
            }

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                await _logging.LogAuthAsync("Register", $"Ошибка регистрации: email {request.Email} уже используется", false);
                return BadRequest(new { message = "Email уже используется" });
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

            await _logging.LogAuthAsync("Register", $"Пользователь {request.Email} успешно зарегистрирован", true,
                $"UserId: {user.Id}, FullName: {user.FullName}");

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
        /// Вход пользователя в систему
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            await _logging.LogAuthAsync("Login", $"Попытка входа для email: {request.Email}", false);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                await _logging.LogAuthAsync("Login", $"Неудачная попытка входа: пользователь {request.Email} не найден", false);
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            if (user.Password != request.Password)
            {
                await _logging.LogAuthAsync("Login", $"Неудачная попытка входа: неверный пароль для {request.Email}", false,
                    $"UserId: {user.Id}");
                return Unauthorized(new { message = "Неверный email или пароль" });
            }

            var token = _jwtService.GenerateToken(user);

            await _logging.LogAuthAsync("Login", $"Пользователь {request.Email} успешно вошел в систему", true,
                $"UserId: {user.Id}, Role: {user.Role}");

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