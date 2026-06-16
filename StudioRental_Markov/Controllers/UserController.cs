using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    /// <summary>
    /// Контроллер для управления данными пользователей.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LoggingService _logging;

        public UsersController(AppDbContext db, LoggingService logging)
        {
            _db = db;
            _logging = logging;
        }

        /// <summary>
        /// Обновление профиля текущего авторизованного пользователя.
        /// Позволяет изменить ФИО, Email и номер телефона.
        /// </summary>
        /// <param name="request">Новые данные профиля.</param>
        /// <returns>Сообщение об успешном обновлении.</returns>
        /// <response code="200">Профиль успешно обновлен.</response>
        /// <response code="400">Ошибка валидации или Email уже используется.</response>
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { message = errors });
            }

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _db.Users.FindAsync(currentUserId);

            if (user == null) return NotFound(new { message = "Пользователь не найден" });

            var existingEmail = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != currentUserId);
            if (existingEmail != null)
            {
                return BadRequest(new { message = "Этот email уже используется другим пользователем" });
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Phone = request.Phone;

            await _db.SaveChangesAsync();
            await _logging.LogUserAsync("UpdateProfile", "Профиль обновлен", currentUserId, $"Новый email: {user.Email}");

            return Ok(new { message = "Профиль успешно обновлен" });
        }

        // ... (остальные методы GetAll, GetById, Delete остаются без изменений) ...
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.Select(u => new { u.Id, u.Email, u.FullName, u.Phone, u.Role, u.CreatedAt }).ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId != id && !User.IsInRole("Admin")) return Forbid();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(new { user.Id, user.Email, user.FullName, user.Phone, user.Role, user.CreatedAt });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Role == "Admin") return BadRequest(new { message = "Нельзя удалить администратора" });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}