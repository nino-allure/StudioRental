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

        public ExportController(ExcelExportService excelExport)
        {
            _excelExport = excelExport;
        }

        [HttpGet("studios")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportStudios()
        {
            var fileContent = await _excelExport.ExportStudiosToExcel();
            return File(fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Студии_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("bookings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportBookings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var fileContent = await _excelExport.ExportBookingsToExcel(startDate, endDate);
            return File(fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Бронирования_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportUsers()
        {
            var fileContent = await _excelExport.ExportUsersToExcel();
            return File(fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Пользователи_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("studio-report/{studioId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportStudioReport(int studioId)
        {
            var fileContent = await _excelExport.ExportStudioReport(studioId);
            return File(fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Отчет_по_студии_{studioId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}