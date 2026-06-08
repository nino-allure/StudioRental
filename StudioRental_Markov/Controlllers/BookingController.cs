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


        // вывод всех
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Studio)
                .ToListAsync();
            return Ok(bookings);
        }


        // вывод Id
        [HttpGet("studio/{studioId}")]
        public async Task<IActionResult> GetByStudio(int studioId)
        {
            var bookings = await _db.Bookings
                .Where(b => b.StudioId == studioId)
                .Include(b => b.Customer)
                .ToListAsync();
            return Ok(bookings);
        }



        // вывод для пользователя
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.Studio)
                .ToListAsync();
            return Ok(bookings);
        }


        // создание бронирования
        [HttpPost]
        public async Task<IActionResult> Create(Booking booking)
        {
            // Проверяем доступность
            var isAvailable = !await _db.Bookings.AnyAsync(b =>
                b.StudioId == booking.StudioId &&
                b.Status != "Canceled" &&
                booking.StartTime < b.EndTime &&
                booking.EndTime > b.StartTime);

            if (!isAvailable)
                return BadRequest("Студия уже забронирована на это время");

            // Получаем студию
            var studio = await _db.Studios.FindAsync(booking.StudioId);
            if (studio == null)
                return BadRequest("Студия не найдена");

            // Рассчитываем стоимость
            var hours = (booking.EndTime - booking.StartTime).TotalHours;
            booking.TotalPrice = (decimal)hours * studio.PricePerHour;
            booking.CreatedAt = DateTime.Now;
            booking.Status = "Pending";

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            return Ok(booking);
        }


        // отмена брони
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Canceled";
            await _db.SaveChangesAsync();
            return Ok(booking);
        }


        // подтверждение
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            booking.Status = "Confirmed";
            await _db.SaveChangesAsync();
            return Ok(booking);
        }
    }
}