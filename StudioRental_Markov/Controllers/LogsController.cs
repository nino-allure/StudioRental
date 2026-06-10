using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
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
        /// Получение списка системных логов с фильтрацией и пагинацией
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? level = null,      
            [FromQuery] string? category = null,   
            [FromQuery] DateTime? from = null,     
            [FromQuery] DateTime? to = null,       
            [FromQuery] int page = 1,              
            [FromQuery] int pageSize = 50)         
        {
            var query = _db.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.LogLevel == level);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            if (from.HasValue)
                query = query.Where(l => l.CreatedAt >= from.Value);

            if (to.HasValue)
            {
                var endDate = to.Value.Date.AddDays(1); // Добавляем 1 день, чтобы включить весь выбранный день
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

            // Логируем просмотр логов
            await _logging.LogInfoAsync("Logs", "GetLogs",
                $"Просмотр логов. Фильтры: level={level}, category={category}, page={page}. Найдено: {totalCount} записей");

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Logs = logs
            });
        }

        /// <summary>
        /// Получение статистики по логам
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var weekAgo = today.AddDays(-7);

            var stats = new
            {
                // Общее количество логов
                TotalLogs = await _db.SystemLogs.CountAsync(),

                // Ошибки за последние 24 часа
                ErrorsLast24h = await _db.SystemLogs
                    .CountAsync(l => l.LogLevel == "ERROR" && l.CreatedAt >= DateTime.Now.AddDays(-1)),

                // Предупреждения за последние 24 часа
                WarningsLast24h = await _db.SystemLogs
                    .CountAsync(l => l.LogLevel == "WARNING" && l.CreatedAt >= DateTime.Now.AddDays(-1)),

                // Количество логов по категориям за последнюю неделю
                LogsByCategory = await _db.SystemLogs
                    .Where(l => l.CreatedAt >= weekAgo)
                    .GroupBy(l => l.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync(),

                // Количество ошибок по категориям за последнюю неделю
                ErrorsByCategory = await _db.SystemLogs
                    .Where(l => l.LogLevel == "ERROR" && l.CreatedAt >= weekAgo)
                    .GroupBy(l => l.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync(),

                // Последние 10 ошибок
                RecentErrors = await _db.SystemLogs
                    .Where(l => l.LogLevel == "ERROR")
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(10)
                    .Select(l => new { l.Id, l.Message, l.Category, CreatedAt = l.CreatedAt })
                    .ToListAsync()
            };

            return Ok(stats);
        }

        /// <summary>
        /// Получение детальной информации о конкретном логе
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLogById(int id)
        {
            var log = await _db.SystemLogs.FindAsync(id);
            if (log == null)
            {
                await _logging.LogWarningAsync("Logs", "GetLogById", $"Лог с ID {id} не найден");
                return NotFound();
            }

            await _logging.LogInfoAsync("Logs", "GetLogById", $"Просмотр деталей лога {id}");
            return Ok(log);
        }

        /// <summary>
        /// Очистка старых логов (старше указанного количества дней)
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysOld = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var logsToDelete = await _db.SystemLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            var deletedCount = logsToDelete.Count;
            _db.SystemLogs.RemoveRange(logsToDelete);
            await _db.SaveChangesAsync();

            await _logging.LogInfoAsync("Logs", "ClearOldLogs",
                $"Очистка старых логов. Удалено {deletedCount} записей старше {daysOld} дней");

            return Ok(new
            {
                DeletedCount = deletedCount,
                Message = $"Удалено {deletedCount} записей старше {daysOld} дней"
            });
        }

        /// <summary>
        /// Экспорт логов в Excel
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportLogs(
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _db.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
                query = query.Where(l => l.LogLevel == level);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            if (from.HasValue)
                query = query.Where(l => l.CreatedAt >= from.Value);

            if (to.HasValue)
            {
                var endDate = to.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt <= endDate);
            }

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // Здесь можно добавить экспорт в Excel через EPPlus
            // Для простоты пока возвращаем JSON с логами

            await _logging.LogInfoAsync("Logs", "ExportLogs",
                $"Экспорт логов. Найдено записей: {logs.Count}");

            return Ok(logs);
        }
    }
}