using System.Linq;
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
        /// Получение списка всех доступных студий с информацией об их владельцах.
        /// Доступно без авторизации.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
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
                        Owner = s.Owner != null ? new
                        {
                            s.Owner.Id,
                            s.Owner.FullName,
                            s.Owner.Email
                        } : null
                    })
                    .ToListAsync();

                await _logging.LogInfoAsync("Studio", "GetAll", $"Получен список студий. Количество: {studios.Count}");

                return Ok(studios);
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Studio", "GetAll", "Ошибка при получении списка студий", ex);
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
            {
                await _logging.LogWarningAsync("Studio", "GetById", $"Студия с ID {id} не найдена");
                return NotFound();
            }

            await _logging.LogInfoAsync("Studio", "GetById", $"Получена информация о студии {id}: {studio.Name}");
            return Ok(studio);
        }

        /// <summary>
        /// Создание новой студии с опциональным изображением
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] StudioCreateDto studioDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await _logging.LogWarningAsync("Studio", "Create", $"Ошибка валидации при создании студии: {errors}");
                return BadRequest(ModelState);
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

            // Обработка загруженного изображения
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
            }
            else if (!string.IsNullOrEmpty(studioDto.ImageUrl))
            {
                studio.ImageUrl = studioDto.ImageUrl;
            }

            _db.Studios.Add(studio);
            await _db.SaveChangesAsync();

            await _logging.LogStudioAsync("Create", $"Создана новая студия", studio.Id,
                $"Название: {studio.Name}, Адрес: {studio.Address}, Цена: {studio.PricePerHour:C}");

            return Ok(studio);
        }


        /// <summary>
        /// Обновление информации о студии
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] StudioUpdateDto studioDto)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
            {
                await _logging.LogWarningAsync("Studio", "Update", $"Попытка обновления несуществующей студии {id}");
                return NotFound(new { message = "Студия не найдена" });
            }

            var oldValues = new
            {
                studio.Name,
                studio.Description,
                studio.Address,
                studio.PricePerHour
            };

            studio.Name = studioDto.Name;
            studio.Description = studioDto.Description;
            studio.Address = studioDto.Address;
            studio.PricePerHour = studioDto.PricePerHour;

            // Обработка нового изображения
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

            await _logging.LogStudioAsync("Update", $"Обновлена студия", id,
                $"Изменения: Название: '{oldValues.Name}' -> '{studio.Name}', " +
                $"Адрес: '{oldValues.Address}' -> '{studio.Address}', " +
                $"Цена: {oldValues.PricePerHour:C} -> {studio.PricePerHour:C}");

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
            {
                await _logging.LogWarningAsync("Studio", "Delete", $"Попытка удаления несуществующей студии {id}");
                return NotFound();
            }

            var hasActiveBookings = await _db.Bookings
                .AnyAsync(b => b.StudioId == id && b.Status != "Cancelled");

            if (hasActiveBookings)
            {
                await _logging.LogWarningAsync("Studio", "Delete",
                    $"Попытка удаления студии {id} ({studio.Name}) с активными бронированиями");
                return BadRequest(new { message = "Нельзя удалить студию с активными бронированиями" });
            }

            var studioName = studio.Name;
            _db.Studios.Remove(studio);
            await _db.SaveChangesAsync();

            await _logging.LogStudioAsync("Delete", $"Удалена студия", id,
                $"Название: {studioName}, Адрес: {studio.Address}");

            return Ok(new { message = "Студия удалена" });
        }
        /// <summary>
        /// Загрузка изображения для студии
        /// </summary>
        [HttpPost("{id}/upload-image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
            {
                await _logging.LogWarningAsync("Studio", "UploadImage", $"Студия с ID {id} не найдена");
                return NotFound(new { message = "Студия не найдена" });
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest(new { message = "Файл не выбран" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Допустимые форматы: JPG, JPEG, PNG, GIF, WEBP" });
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Максимальный размер файла 5MB" });
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);

            studio.ImageData = memoryStream.ToArray();
            studio.ImageContentType = image.ContentType;
            studio.ImageUrl = null; // Очищаем URL, так как используем бинарные данные

            await _db.SaveChangesAsync();

            await _logging.LogStudioAsync("UploadImage", $"Загружено изображение для студии", id,
                $"Имя файла: {image.FileName}, Размер: {image.Length} байт");

            return Ok(new { message = "Изображение загружено" });
        }

        /// <summary>
        /// Получение изображения студии
        /// </summary>
        [HttpGet("{id}/image")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(int id)
        {
            var studio = await _db.Studios.FindAsync(id);
            if (studio == null)
            {
                return NotFound();
            }

            if (studio.ImageData != null && studio.ImageContentType != null)
            {
                return File(studio.ImageData, studio.ImageContentType);
            }

            // Возвращаем изображение-заглушку
            var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "studio-default.jpg");
            if (System.IO.File.Exists(defaultImagePath))
            {
                var defaultImage = await System.IO.File.ReadAllBytesAsync(defaultImagePath);
                return File(defaultImage, "image/jpeg");
            }

            return NotFound();
        }

    }
}