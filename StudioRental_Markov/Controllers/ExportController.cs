using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioRental_Markov.Services;

namespace StudioRental_Markov.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly ExcelExportService _excelExport;
        private readonly LoggingService _logging; // Сервис логирования

        public ExportController(ExcelExportService excelExport, LoggingService logging)
        {
            _excelExport = excelExport;
            _logging = logging; // Инициализация сервиса логирования
        }

        /// <summary>
        /// Экспорт списка студий в Excel (только для администраторов)
        /// </summary>
        [HttpGet("studios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportStudios()
        {
            try
            {
                var fileContent = await _excelExport.ExportStudiosToExcel();

                // Логируем экспорт студий
                await _logging.LogInfoAsync("Export", "ExportStudios",
                    $"Экспорт списка студий в Excel. Размер файла: {fileContent.Length} байт");

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Студии_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Export", "ExportStudios", "Ошибка при экспорте студий", ex);
                return StatusCode(500, new { message = "Ошибка при экспорте данных" });
            }
        }

        /// <summary>
        /// Экспорт списка бронирований в Excel (только для администраторов)
        /// </summary>
        [HttpGet("bookings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportBookings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var fileContent = await _excelExport.ExportBookingsToExcel(startDate, endDate);

                // Логируем экспорт бронирований с параметрами фильтрации
                var filterInfo = startDate.HasValue || endDate.HasValue
                    ? $" (Фильтр: с {startDate:yyyy-MM-dd} по {endDate:yyyy-MM-dd})"
                    : "";

                await _logging.LogInfoAsync("Export", "ExportBookings",
                    $"Экспорт списка бронирований в Excel{filterInfo}. Размер файла: {fileContent.Length} байт");

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Бронирования_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Export", "ExportBookings", "Ошибка при экспорте бронирований", ex);
                return StatusCode(500, new { message = "Ошибка при экспорте данных" });
            }
        }

        /// <summary>
        /// Экспорт списка пользователей в Excel (только для администраторов)
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportUsers()
        {
            try
            {
                var fileContent = await _excelExport.ExportUsersToExcel();

                await _logging.LogInfoAsync("Export", "ExportUsers",
                    $"Экспорт списка пользователей в Excel. Размер файла: {fileContent.Length} байт");

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Пользователи_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Export", "ExportUsers", "Ошибка при экспорте пользователей", ex);
                return StatusCode(500, new { message = "Ошибка при экспорте данных" });
            }
        }

        /// <summary>
        /// Экспорт детального отчета по студии (только для администраторов)
        /// </summary>
        [HttpGet("studio-report/{studioId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportStudioReport(int studioId)
        {
            try
            {
                var fileContent = await _excelExport.ExportStudioReport(studioId);

                await _logging.LogInfoAsync("Export", "ExportStudioReport",
                    $"Экспорт отчета по студии {studioId}. Размер файла: {fileContent.Length} байт");

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Отчет_по_студии_{studioId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Export", "ExportStudioReport",
                    $"Ошибка при экспорте отчета по студии {studioId}", ex);
                return StatusCode(500, new { message = "Ошибка при экспорте данных" });
            }
        }
    }
}