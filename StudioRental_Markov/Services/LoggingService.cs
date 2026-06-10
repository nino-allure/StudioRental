using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using System.Text;

namespace StudioRental_Markov.Services
{
    public class LoggingService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly object _fileLock = new object();

        public LoggingService(AppDbContext db, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip) || ip == "::1")
                ip = "127.0.0.1";

            return ip;
        }

        private string GetRequestPath()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";
            return $"{context.Request.Path}{context.Request.QueryString}";
        }

        private string GetRequestMethod()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";
            return context.Request.Method;
        }

        private int? GetCurrentUserId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User == null) return null;

            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return null;
        }

        private string? GetCurrentUserEmail()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User == null) return null;

            return context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Запись лога в БД и файл
        /// </summary>
        public async Task LogAsync(string logLevel, string category, string action, string message,
            string? details = null, Exception? exception = null)
        {
            var logEntry = new SystemLog
            {
                LogLevel = logLevel,
                Category = category,
                Action = action,
                Message = message,
                Details = details ?? (exception?.ToString()),
                UserId = GetCurrentUserId(),
                UserEmail = GetCurrentUserEmail(),
                IpAddress = GetCurrentIpAddress(),
                RequestPath = GetRequestPath(),
                RequestMethod = GetRequestMethod(),
                CreatedAt = DateTime.Now
            };

            // Сохраняем в БД
            try
            {
                _db.SystemLogs.Add(logEntry);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Если БД недоступна, хотя бы в файл запишем
                await WriteToFileAsync(logEntry, ex);
            }

            // Также пишем в файл
            await WriteToFileAsync(logEntry);
        }

        private async Task WriteToFileAsync(SystemLog log, Exception? dbException = null)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                var fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
                var filePath = Path.Combine(logDir, fileName);

                var logText = new StringBuilder();
                logText.AppendLine(new string('=', 80));
                logText.AppendLine($"Время: {log.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}");
                logText.AppendLine($"Уровень: {log.LogLevel}");
                logText.AppendLine($"Категория: {log.Category}");
                logText.AppendLine($"Действие: {log.Action}");
                logText.AppendLine($"Сообщение: {log.Message}");
                logText.AppendLine($"Пользователь: ID={log.UserId}, Email={log.UserEmail}");
                logText.AppendLine($"IP: {log.IpAddress}");
                logText.AppendLine($"Запрос: {log.RequestMethod} {log.RequestPath}");
                if (!string.IsNullOrEmpty(log.Details))
                    logText.AppendLine($"Детали: {log.Details}");
                if (dbException != null)
                    logText.AppendLine($"Ошибка БД: {dbException.Message}");
                logText.AppendLine(new string('=', 80));

                lock (_fileLock)
                {
                    System.IO.File.AppendAllText(filePath, logText.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось записать лог в файл: {ex.Message}");
            }
        }

        // Удобные методы-обертки
        public Task LogInfoAsync(string category, string action, string message, string? details = null)
            => LogAsync("INFO", category, action, message, details);

        public Task LogWarningAsync(string category, string action, string message, string? details = null)
            => LogAsync("WARNING", category, action, message, details);

        public Task LogErrorAsync(string category, string action, string message, Exception? exception = null, string? details = null)
            => LogAsync("ERROR", category, action, message, details, exception);

        // Логи для конкретных категорий
        public Task LogAuthAsync(string action, string message, bool isSuccess = true, string? details = null)
            => LogAsync(isSuccess ? "INFO" : "WARNING", "Auth", action, message, details);

        public Task LogBookingAsync(string action, string message, int? bookingId = null, string? details = null)
            => LogAsync("INFO", "Booking", action, $"{message} (BookingId: {bookingId})", details);

        public Task LogStudioAsync(string action, string message, int? studioId = null, string? details = null)
            => LogAsync("INFO", "Studio", action, $"{message} (StudioId: {studioId})", details);

        public Task LogUserAsync(string action, string message, int? userId = null, string? details = null)
            => LogAsync("INFO", "User", action, $"{message} (UserId: {userId})", details);

        public Task LogErrorWithException(string category, string action, Exception ex, string? additionalInfo = null)
            => LogErrorAsync(category, action, additionalInfo ?? ex.Message, ex);
    }
}