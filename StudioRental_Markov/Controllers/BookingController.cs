using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    /// <summary>
    /// Контроллер для управления бронированиями студий.
    /// </summary>
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
        /// Получение списка всех бронирований в системе (Только для Администраторов).
        /// </summary>
        /// <returns>Список всех бронирований с данными клиентов и студий.</returns>
        /// <response code="200">Список успешно получен.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="403">Недостаточно прав (требуется роль Admin).</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var bookings = await _db.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Studio)
                    .Select(b => new
                    {
                        b.Id,
                        b.StartTime,
                        b.EndTime,
                        b.TotalPrice,
                        b.Status,
                        b.CreatedAt,
                        b.CustomerId,
                        b.StudioId,
                        Customer = b.Customer != null ? new { b.Customer.Id, b.Customer.FullName, b.Customer.Email, b.Customer.Phone } : null,
                        Studio = b.Studio != null ? new { b.Studio.Id, b.Studio.Name, b.Studio.Address, b.Studio.PricePerHour } : null
                    })
                    .ToListAsync();

                await _logging.LogInfoAsync("Booking", "GetAll", $"Получен список бронирований. Количество: {bookings.Count}");
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Booking", "GetAll", "Ошибка при получении списка бронирований", ex);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение списка бронирований конкретного пользователя.
        /// Пользователь может видеть только свои бронирования, администратор - любые.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Список бронирований указанного пользователя.</returns>
        /// <response code="200">Список успешно получен.</response>
        /// <response code="403">Попытка доступа к чужим бронированиям без прав администратора.</response>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                await _logging.LogWarningAsync("Booking", "GetByUser", $"Попытка несанкционированного доступа к бронированиям пользователя {userId} от пользователя {currentUserId}");
                return Forbid();
            }

            var bookings = await _db.Bookings
                .Where(b => b.CustomerId == userId)
                .Include(b => b.Studio)
                .ToListAsync();

            await _logging.LogInfoAsync("Booking", "GetByUser", $"Пользователь {currentUserId} запросил свои бронирования. Найдено: {bookings.Count}");
            return Ok(bookings);
        }

        /// <summary>
        /// Создание нового бронирования студии.
        /// </summary>
        /// <param name="booking">Данные о бронировании (StudioId, StartTime, EndTime).</param>
        /// <returns>Созданное бронирование с рассчитанной стоимостью.</returns>
        /// <response code="200">Бронирование успешно создано.</response>
        /// <response code="400">Ошибка в данных (время в прошлом, пересечение с другими бронированиями).</response>
        [HttpPost]
        [ProducesResponseType(typeof(Booking), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] Booking booking)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
            {
                await _logging.LogWarningAsync("Booking", "Create", $"Пользователь {currentUserId} попытался создать бронирование для другого пользователя {booking.CustomerId}");
                return Forbid();
            }

            var studio = await _db.Studios.FindAsync(booking.StudioId);
            if (studio == null)
            {
                await _logging.LogWarningAsync("Booking", "Create", $"Попытка бронирования несуществующей студии. StudioId: {booking.StudioId}");
                return BadRequest(new { message = "Студия не найдена" });
            }

            if (booking.StartTime >= booking.EndTime)
                return BadRequest(new { message = "Дата начала должна быть строго меньше даты окончания" });

            if (booking.StartTime < DateTime.Now)
                return BadRequest(new { message = "Нельзя бронировать время в прошлом" });

            var conflictingBooking = await _db.Bookings
                .AnyAsync(b => b.StudioId == booking.StudioId
                     && b.Status != "Cancelled"
                     && b.StartTime < booking.EndTime
                     && b.EndTime > booking.StartTime);

            if (conflictingBooking)
                return BadRequest(new { message = "Выбранное время уже занято другой бронью" });

            var duration = (booking.EndTime - booking.StartTime).TotalHours;
            booking.TotalPrice = (decimal)duration * studio.PricePerHour;
            booking.Status = "Pending";
            booking.CreatedAt = DateTime.Now;

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Create", "Создано новое бронирование", booking.Id,
                $"Студия: {studio.Name}, Клиент: {currentUserId}, Сумма: {booking.TotalPrice:C}");

            return Ok(booking);
        }

        /// <summary>
        /// Отмена бронирования (доступно владельцу брони или администратору).
        /// </summary>
        /// <param name="id">Идентификатор бронирования.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Бронирование успешно отменено.</response>
        /// <response code="400">Нельзя отменить подтвержденную бронь менее чем за 2 часа до начала.</response>
        /// <response code="404">Бронирование не найдено.</response>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings.Include(b => b.Studio).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound(new { message = "Бронирование не найдено" });

            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (booking.CustomerId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            if (booking.Status == "Confirmed" && booking.StartTime < DateTime.Now.AddHours(2))
                return BadRequest(new { message = "Нельзя отменить подтвержденное бронирование менее чем за 2 часа до начала" });

            var oldStatus = booking.Status;
            booking.Status = "Cancelled";
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Cancel", "Бронирование отменено", id, $"Статус изменен с {oldStatus} на Cancelled");
            return Ok(new { message = "Бронирование успешно отменено" });
        }

        /// <summary>
        /// Подтверждение бронирования (Только для Администраторов).
        /// </summary>
        /// <param name="id">Идентификатор бронирования.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Бронирование подтверждено.</response>
        /// <response code="400">Бронирование уже имеет другой статус.</response>
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _db.Bookings.Include(b => b.Studio).Include(b => b.Customer).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound(new { message = "Бронирование не найдено" });

            if (booking.Status != "Pending")
                return BadRequest(new { message = $"Нельзя подтвердить бронирование со статусом {booking.Status}" });

            booking.Status = "Confirmed";
            await _db.SaveChangesAsync();

            await _logging.LogBookingAsync("Confirm", "Бронирование подтверждено администратором", id);
            return Ok(new { message = "Бронирование успешно подтверждено" });
        }
    }
}