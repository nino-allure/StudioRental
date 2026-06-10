using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Требуем авторизацию
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
        [Authorize(Roles = "Admin")]  // Только для админов
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
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Пользователь может видеть только свои бронирования, админ - любые
            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.Studio)
                .ToListAsync();
            return Ok(bookings);
        }

        /// <summary>
        /// Создание нового бронирования
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Booking booking)
        {
            // Проверяем, что пользователь бронирует для себя
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Расчет стоимости
            var duration = (booking.EndTime - booking.StartTime).TotalHours;
            var studio = await _db.Studios.FindAsync(booking.StudioId);
            if (studio == null)
                return BadRequest("Студия не найдена");

            booking.TotalPrice = (decimal)duration * studio.PricePerHour;
            booking.Status = "Pending";
            booking.CreatedAt = DateTime.Now;

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            return Ok(booking);
        }

        /// <summary>
        /// Отмена бронирования
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            booking.Status = "Cancelled";
            await _db.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Подтверждение бронирования (только админ)
        /// </summary>
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Confirmed";
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}