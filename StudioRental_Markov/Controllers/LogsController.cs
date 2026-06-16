using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    /// <summary>
    /// Контроллер для управления системными логами (Только для Администраторов).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LoggingService _logging;

        public LogsController(AppDbContext db, LoggingService logging)
        {
            _db = db;
            _logging = logging;
        }

        /// <summary>
        /// Получение списка системных логов с фильтрацией и пагинацией.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _db.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level)) query = query.Where(l => l.LogLevel == level);
            if (!string.IsNullOrEmpty(category)) query = query.Where(l => l.Category == category);
            if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
            if (to.HasValue)
            {
                var endDate = to.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt <= endDate);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.LogLevel,
                    l.Category,
                    l.Action,
                    l.Message,
                    l.Details,
                    l.UserId,
                    l.UserEmail,
                    l.IpAddress,
                    l.RequestPath,
                    l.RequestMethod,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { TotalCount = totalCount, Page = page, PageSize = pageSize, Logs = logs });
        }

        /// <summary>
        /// Получение агрегированной статистики по системным логам.
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var weekAgo = today.AddDays(-7);

            var stats = new
            {
                TotalLogs = await _db.SystemLogs.CountAsync(),
                ErrorsLast24h = await _db.SystemLogs.CountAsync(l => l.LogLevel == "ERROR" && l.CreatedAt >= DateTime.Now.AddDays(-1)),
                WarningsLast24h = await _db.SystemLogs.CountAsync(l => l.LogLevel == "WARNING" && l.CreatedAt >= DateTime.Now.AddDays(-1)),
                LogsByCategory = await _db.SystemLogs.Where(l => l.CreatedAt >= weekAgo).GroupBy(l => l.Category).Select(g => new { Category = g.Key, Count = g.Count() }).ToListAsync(),
                ErrorsByCategory = await _db.SystemLogs.Where(l => l.LogLevel == "ERROR" && l.CreatedAt >= weekAgo).GroupBy(l => l.Category).Select(g => new { Category = g.Key, Count = g.Count() }).ToListAsync()
            };

            return Ok(stats);
        }

        /// <summary>
        /// Очистка старых логов (старше указанного количества дней).
        /// </summary>
        /// <param name="daysOld">Количество дней, старше которых логи будут удалены (по умолчанию 30).</param>
        [HttpDelete("clear")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysOld = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var logsToDelete = await _db.SystemLogs.Where(l => l.CreatedAt < cutoffDate).ToListAsync();

            var deletedCount = logsToDelete.Count;
            _db.SystemLogs.RemoveRange(logsToDelete);
            await _db.SaveChangesAsync();

            await _logging.LogInfoAsync("Logs", "ClearOldLogs", $"Удалено {deletedCount} записей старше {daysOld} дней");
            return Ok(new { DeletedCount = deletedCount, Message = $"Удалено {deletedCount} записей" });
        }

        /// <summary>
        /// Получение отфильтрованного списка логов в формате JSON (для последующего экспорта на клиенте).
        /// Примечание: Для прямого скачивания Excel-файла используйте соответствующий метод в ExportController.
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ExportLogs(
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _db.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level)) query = query.Where(l => l.LogLevel == level);
            if (!string.IsNullOrEmpty(category)) query = query.Where(l => l.Category == category);
            if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value.Date.AddDays(1));

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
            return Ok(logs);
        }
    }
}