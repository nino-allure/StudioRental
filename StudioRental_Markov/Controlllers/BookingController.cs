using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BookingsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Получение списка всех бронирований в системе с информацией о клиентах и студиях.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var bookings = await _db.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Studio)
                    .ToListAsync();

                Console.WriteLine($"Found {bookings.Count} bookings");
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение списка всех бронирований конкретного пользователя по его идентификатору.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.Studio)
                .ToListAsync();
            return Ok(bookings);
        }
    }
}
