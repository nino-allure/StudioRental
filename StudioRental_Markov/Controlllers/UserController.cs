using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

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


        // получение всех
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.ToListAsync();
            return Ok(users);
        }




        [HttpGet("{id}")]
        // получение по Id
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }


        
        [HttpPost("register")]
        // регистрация
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



        [HttpDelete("{id}")]
        //удаление
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