using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] 
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
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

                Console.WriteLine($"Found {users.Count} users");
                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
                return NotFound();

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
        /// Удаление пользователя
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (user.Role == "Admin")
                return BadRequest("Нельзя удалить администратора");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}