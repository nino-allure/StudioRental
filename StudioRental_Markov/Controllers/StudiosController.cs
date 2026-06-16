using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    /// <summary>
    /// Контроллер для управления данными студий звукозаписи.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StudiosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LoggingService _logging;

        public StudiosController(AppDbContext db, LoggingService logging)
        {
            _db = db;
            _logging = logging;
        }

        /// <summary>
        /// Получение изображения студии по её ID.
        /// </summary>
        /// <param name="id">Идентификатор студии.</param>
        /// <returns>Файл изображения или заглушка, если изображение не загружено.</returns>
        [HttpGet("{id}/image")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetImage(int id)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null) return NotFound();

            if (studio.ImageData != null && studio.ImageContentType != null)
                return File(studio.ImageData, studio.ImageContentType);

            var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "studio-default.jpg");
            if (System.IO.File.Exists(defaultImagePath))
            {
                var defaultImage = await System.IO.File.ReadAllBytesAsync(defaultImagePath);
                return File(defaultImage, "image/jpeg");
            }

            return NotFound();
        }

        /// <summary>
        /// Получение списка всех доступных студий (доступно без авторизации).
        /// </summary>
        /// <returns>Список студий с базовой информацией и данными владельца.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var studios = await _db.Studios
                    .Include(s => s.Owner)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.Address,
                        s.PricePerHour,
                        s.ImageUrl,
                        s.IsApproved,
                        s.CreatedAt,
                        s.OwnerId,
                        Owner = s.Owner != null ? new { s.Owner.Id, s.Owner.FullName, s.Owner.Email } : null
                    })
                    .ToListAsync();

                return Ok(studios);
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Studio", "GetAll", "Ошибка при получении списка студий", ex);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Создание новой студии с возможностью загрузки изображения (Только для Администраторов).
        /// </summary>
        /// <param name="studioDto">Данные студии и опциональный файл изображения.</param>
        /// <returns>Созданная студия.</returns>
        /// <response code="200">Студия успешно создана.</response>
        /// <response code="400">Ошибка валидации данных.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Studio), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromForm] StudioCreateDto studioDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await _logging.LogWarningAsync("Studio", "Create", $"Ошибка валидации: {errors}");
                return BadRequest(new { message = errors });
            }

            var ownerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var studio = new Studio
            {
                Name = studioDto.Name,
                Description = studioDto.Description,
                Address = studioDto.Address,
                PricePerHour = studioDto.PricePerHour,
                OwnerId = ownerId,
                CreatedAt = DateTime.Now,
                IsApproved = true
            };

            if (studioDto.Image != null && studioDto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(studioDto.Image.FileName).ToLowerInvariant();

                if (allowedExtensions.Contains(extension) && studioDto.Image.Length <= 5 * 1024 * 1024)
                {
                    using var memoryStream = new MemoryStream();
                    await studioDto.Image.CopyToAsync(memoryStream);
                    studio.ImageData = memoryStream.ToArray();
                    studio.ImageContentType = studioDto.Image.ContentType;
                }
                else
                {
                    return BadRequest(new { message = "Недопустимый формат или размер файла (макс. 5MB, форматы: JPG, PNG, GIF, WEBP)" });
                }
            }
            else if (!string.IsNullOrEmpty(studioDto.ImageUrl))
            {
                studio.ImageUrl = studioDto.ImageUrl;
            }

            _db.Studios.Add(studio);
            await _db.SaveChangesAsync();

            await _logging.LogStudioAsync("Create", "Создана новая студия", studio.Id);
            return Ok(studio);
        }

        /// <summary>
        /// Обновление информации о студии (Только для Администраторов).
        /// </summary>
        /// <param name="id">Идентификатор студии.</param>
        /// <param name="studioDto">Обновленные данные студии.</param>
        /// <returns>Обновленная студия.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Studio), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, [FromForm] StudioUpdateDto studioDto)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null) return NotFound(new { message = "Студия не найдена" });

            studio.Name = studioDto.Name;
            studio.Description = studioDto.Description;
            studio.Address = studioDto.Address;
            studio.PricePerHour = studioDto.PricePerHour;

            if (studioDto.Image != null && studioDto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(studioDto.Image.FileName).ToLowerInvariant();

                if (allowedExtensions.Contains(extension) && studioDto.Image.Length <= 5 * 1024 * 1024)
                {
                    using var memoryStream = new MemoryStream();
                    await studioDto.Image.CopyToAsync(memoryStream);
                    studio.ImageData = memoryStream.ToArray();
                    studio.ImageContentType = studioDto.Image.ContentType;
                    studio.ImageUrl = null;
                }
            }
            else if (studioDto.RemoveImage == true)
            {
                studio.ImageData = null;
                studio.ImageContentType = null;
                studio.ImageUrl = null;
            }
            else if (!string.IsNullOrEmpty(studioDto.ImageUrl))
            {
                studio.ImageUrl = studioDto.ImageUrl;
                studio.ImageData = null;
                studio.ImageContentType = null;
            }

            await _db.SaveChangesAsync();
            await _logging.LogStudioAsync("Update", "Обновлена студия", id);
            return Ok(studio);
        }

        /// <summary>
        /// Удаление студии (Только для Администраторов).
        /// </summary>
        /// <param name="id">Идентификатор студии.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Студия успешно удалена.</response>
        /// <response code="400">Нельзя удалить студию, если у неё есть активные бронирования.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Delete(int id)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null) return NotFound();

            var hasActiveBookings = await _db.Bookings.AnyAsync(b => b.StudioId == id && b.Status != "Cancelled");
            if (hasActiveBookings)
                return BadRequest(new { message = "Нельзя удалить студию с активными бронированиями" });

            _db.Studios.Remove(studio);
            await _db.SaveChangesAsync();

            await _logging.LogStudioAsync("Delete", "Удалена студия", id);
            return Ok(new { message = "Студия успешно удалена" });
        }
    }
}