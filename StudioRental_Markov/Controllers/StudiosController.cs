using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudiosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StudiosController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Получение списка всех доступных студий с информацией об их владельцах.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var studios = await _db.Studios
                    .Include(s => s.Owner)
                    .ToListAsync();

                Console.WriteLine($"Found {studios.Count} studios");
                return Ok(studios);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получение детальной информации о конкретной студии по её идентификатору.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var studio = await _db.Studios
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (studio == null)
                return NotFound();

            return Ok(studio);
        }

        /// <summary>
        /// Создание новой студии (только для администраторов).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Studio studio)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Получаем ID текущего пользователя из токена
            var ownerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            studio.OwnerId = ownerId;
            studio.CreatedAt = DateTime.Now;
            studio.IsApproved = true; // Админ создает уже подтвержденную студию

            _db.Studios.Add(studio);
            await _db.SaveChangesAsync();

            return Ok(studio);
        }

        /// <summary>
        /// Обновление информации о студии (только для администраторов).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Studio updatedStudio)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
                return NotFound();

            studio.Name = updatedStudio.Name;
            studio.Description = updatedStudio.Description;
            studio.Address = updatedStudio.Address;
            studio.PricePerHour = updatedStudio.PricePerHour;
            studio.ImageUrl = updatedStudio.ImageUrl;

            await _db.SaveChangesAsync();

            return Ok(studio);
        }

        /// <summary>
        /// Удаление студии (только для администраторов).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
                return NotFound();

            // Проверяем, есть ли активные бронирования
            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.StudioId == id && b.Status != "Cancelled");

            if (hasActiveBookings)
                return BadRequest(new { message = "Нельзя удалить студию с активными бронированиями" });

            _db.Studios.Remove(studio);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Студия удалена" });
        }
    }
}