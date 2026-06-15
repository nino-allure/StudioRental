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
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LoggingService _logging; 

        public BookingsController(AppDbContext db, LoggingService logging)
        {
            _db = db;
            _logging = logging; 
        }

        /// <summary>
        /// Получение списка всех бронирований в системе с информацией о клиентах и студиях.
        /// Доступно только для администраторов.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var bookings = await _db.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Studio)
                    .ToListAsync();

                await _logging.LogInfoAsync("Booking", "GetAll", $"Получен список бронирований. Количество: {bookings.Count}");

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Booking", "GetAll", "Ошибка при получении списка бронирований", ex);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение списка всех бронирований конкретного пользователя по его идентификатору.
        /// Пользователь может видеть только свои бронирования, админ - любые.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                await _logging.LogWarningAsync("Booking", "GetByUser",
                    $"Попытка несанкционированного доступа к бронированиям пользователя {userId} от пользователя {currentUserId}");
                return Forbid();
            }

            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.Studio)
                .ToListAsync();

            await _logging.LogInfoAsync("Booking", "GetByUser",
                $"Пользователь {currentUserId} запросил свои бронирования. Найдено: {bookings.Count}");

            return Ok(bookings);
        }

        /// <summary>
        /// Создание нового бронирования
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Booking booking)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
            {
                await _logging.LogWarningAsync("Booking", "Create",
                    $"Пользователь {currentUserId} попытался создать бронирование для другого пользователя {booking.CustomerId}");
                return Forbid();
            }

            var studio = await _db.Studios.FindAsync(booking.StudioId);
            if (studio == null)
            {
                await _logging.LogWarningAsync("Booking", "Create",
                    $"Попытка бронирования несуществующей студии. StudioId: {booking.StudioId}, UserId: {currentUserId}");
                return BadRequest("Студия не найдена");
            }

            if (booking.StartTime >= booking.EndTime)
            {
                await _logging.LogWarningAsync("Booking", "Create",
                    $"Некорректные даты бронирования. StartTime: {booking.StartTime}, EndTime: {booking.EndTime}");
                return BadRequest("Дата начала должна быть меньше даты окончания");
            }

            if (booking.StartTime < DateTime.Now)
            {
                await _logging.LogWarningAsync("Booking", "Create",
                    $"Попытка бронирования в прошлом. StartTime: {booking.StartTime}");
                return BadRequest("Нельзя бронировать время в прошлом");
            }

            var conflictingBooking = await _db.Bookings
                .AnyAsync(b => b.StudioId == booking.StudioId
                    && b.Status != "Cancelled"
                    && b.StartTime < booking.EndTime
                    && b.EndTime > booking.StartTime);

            if (conflictingBooking)
            {
                await _logging.LogWarningAsync("Booking", "Create",
                    $"Конфликт бронирований. StudioId: {booking.StudioId}, Время: {booking.StartTime} - {booking.EndTime}");
                return BadRequest("Выбранное время уже занято");
            }

            var duration = (booking.EndTime - booking.StartTime).TotalHours;
            booking.TotalPrice = (decimal)duration * studio.PricePerHour;
            booking.Status = "Pending";
            booking.CreatedAt = DateTime.Now;

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Create", $"Создано новое бронирование", booking.Id,
                $"Студия: {studio.Name} (Id: {booking.StudioId}), " +
                $"Клиент: {currentUserId}, " +
                $"Время: {booking.StartTime:yyyy-MM-dd HH:mm} - {booking.EndTime:yyyy-MM-dd HH:mm}, " +
                $"Длительность: {duration:F1}ч, " +
                $"Сумма: {booking.TotalPrice:C}");

            return Ok(booking);
        }

        /// <summary>
        /// Отмена бронирования (пользователь или админ)
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.Studio)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                await _logging.LogWarningAsync("Booking", "Cancel", $"Попытка отмены несуществующего бронирования {id}");
                return NotFound();
            }

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
            {
                await _logging.LogWarningAsync("Booking", "Cancel",
                    $"Пользователь {currentUserId} попытался отменить чужое бронирование {id}");
                return Forbid();
            }

            if (booking.Status == "Confirmed" && booking.StartTime < DateTime.Now.AddHours(2))
            {
                await _logging.LogWarningAsync("Booking", "Cancel",
                    $"Попытка отмены подтвержденного бронирования {id} менее чем за 2 часа до начала");
                return BadRequest("Нельзя отменить подтвержденное бронирование менее чем за 2 часа до начала");
            }

            var oldStatus = booking.Status;
            booking.Status = "Cancelled";
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Cancel", $"Бронирование отменено", id,
                $"Студия: {booking.Studio?.Name}, " +
                $"Статус изменен с {oldStatus} на Cancelled, " +
                $"Пользователь: {currentUserId}");

            return Ok();
        }

        /// <summary>
        /// Подтверждение бронирования (только администратор)
        /// </summary>
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.Studio)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                await _logging.LogWarningAsync("Booking", "Confirm", $"Попытка подтверждения несуществующего бронирования {id}");
                return NotFound();
            }

            if (booking.Status != "Pending")
            {
                await _logging.LogWarningAsync("Booking", "Confirm",
                    $"Попытка подтверждения бронирования {id} с некорректным статусом {booking.Status}");
                return BadRequest($"Нельзя подтвердить бронирование со статусом {booking.Status}");
            }

            var oldStatus = booking.Status;
            booking.Status = "Confirmed";
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Confirm", $"Бронирование подтверждено администратором", id,
                $"Студия: {booking.Studio?.Name}, " +
                $"Клиент: {booking.Customer?.FullName} (Id: {booking.CustomerId}), " +
                $"Время: {booking.StartTime:yyyy-MM-dd HH:mm} - {booking.EndTime:yyyy-MM-dd HH:mm}, " +
                $"Сумма: {booking.TotalPrice:C}");

            return Ok();
        }
    }
}