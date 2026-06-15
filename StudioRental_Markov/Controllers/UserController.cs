using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] 
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
        /// Получение списка всех зарегистрированных пользователей без отображения их паролей.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _db.Users
                    .Select(u => new {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.Phone,
                        u.Role,
                        u.CreatedAt
                    })
                    .ToListAsync();

                await _logging.LogInfoAsync("User", "GetAll", $"Получен список пользователей. Количество: {users.Count}");

                return Ok(users);
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("User", "GetAll", "Ошибка при получении списка пользователей", ex);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение профиля конкретного пользователя по его идентификатору (без пароля).
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                await _logging.LogWarningAsync("User", "GetById", $"Пользователь с ID {id} не найден");
                return NotFound();
            }

            await _logging.LogInfoAsync("User", "GetById", $"Получена информация о пользователе {id}: {user.Email}");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.Phone,
                user.Role,
                user.CreatedAt
            });
        }

        /// <summary>
        /// Удаление пользователя (нельзя удалить администратора)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                await _logging.LogWarningAsync("User", "Delete", $"Попытка удаления несуществующего пользователя {id}");
                return NotFound();
            }

            if (user.Role == "Admin")
            {
                await _logging.LogWarningAsync("User", "Delete",
                    $"Попытка удаления администратора {user.Email} (ID: {id})");
                return BadRequest("Нельзя удалить администратора");
            }

            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.CustomerId == id && b.Status != "Cancelled");

            if (hasActiveBookings)
            {
                await _logging.LogWarningAsync("User", "Delete",
                    $"Попытка удаления пользователя {user.Email} (ID: {id}) с активными бронированиями");
                return BadRequest("Нельзя удалить пользователя с активными бронированиями");
            }

            var userInfo = $"{user.FullName} ({user.Email})";
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            await _logging.LogUserAsync("Delete", $"Удален пользователь", id,
                $"Информация: {userInfo}, Роль: {user.Role}");

            return Ok();
        }
    }
}