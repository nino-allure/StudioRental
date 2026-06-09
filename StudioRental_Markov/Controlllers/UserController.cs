using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using Swashbuckle.Swagger.Annotations;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
        }


        /// <summary>
        /// Вывод всех пользователей
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.ToListAsync();
            return Ok(users);
        }



        /// <summary>
        /// Вывод пользователей по Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }


        /// <summary>
        /// Регистрация пользователя
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            // Проверяем email
            var exists = await _db.Users.AnyAsync(u => u.Email == user.Email);
            if (exists)
                return BadRequest("Email уже используется");

            user.CreatedAt = DateTime.Now;
            user.Password = user.Password;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(user);
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

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}