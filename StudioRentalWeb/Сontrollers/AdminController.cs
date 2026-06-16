using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _api;
        private readonly NotificationService _notifications;

        public AdminController(ApiService api, NotificationService notifications)
        {
            _api = api;
            _notifications = notifications;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var token = HttpContext.Session.GetString("JwtToken");
            return role == "Admin" && !string.IsNullOrEmpty(token);
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (users, _) = await _api.GetAsync<List<User>>("Users");
            var (studios, _) = await _api.GetAsync<List<Studio>>("Studios");
            var (bookings, _) = await _api.GetAsync<List<Booking>>("Bookings");

            ViewBag.UsersCount = users?.Count ?? 0;
            ViewBag.StudiosCount = studios?.Count ?? 0;
            ViewBag.BookingsCount = bookings?.Count ?? 0;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (users, error) = await _api.GetAsync<List<User>>("Users");
            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при загрузке пользователей");
            return View(users ?? new List<User>());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (success, error) = await _api.DeleteAsync($"Users/{id}");

            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при удалении пользователя");
            else _notifications.AddSuccess(this, "Пользователь удален");

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Studios()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (studios, error) = await _api.GetAsync<List<Studio>>("Studios");
            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при загрузке студий");
            return View(studios ?? new List<Studio>());
        }

        [HttpGet]
        public IActionResult CreateStudio()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStudio(StudioViewModel model, IFormFile? image)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(model);

            // Свойства должны точно совпадать с StudioCreateDto на бэкенде
            var studioData = new
            {
                name = model.Name,
                description = model.Description ?? "",
                address = model.Address,
                pricePerHour = model.PricePerHour,
                imageUrl = model.ImageUrl ?? ""
            };

            var (result, error) = await _api.PostWithFileAsync<Studio>("Studios", studioData, image);

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при создании студии");
                return View(model);
            }

            _notifications.AddSuccess(this, $"Студия \"{model.Name}\" успешно создана");
            return RedirectToAction("Studios");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudio(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (success, error) = await _api.DeleteAsync($"Studios/{id}");

            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при удалении студии");
            else _notifications.AddSuccess(this, "Студия удалена");

            return RedirectToAction("Studios");
        }

        [HttpGet]
        [Route("Admin/EditStudio/{id}")]
        public async Task<IActionResult> EditStudio(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (studio, apiError) = await _api.GetAsync<Studio>($"Studios/{id}");

            if (apiError != null || studio == null)
            {
                _notifications.AddError(this, "Студия не найдена");
                return RedirectToAction("Studios");
            }

            var model = new StudioViewModel
            {
                Id = studio.Id,
                Name = studio.Name,
                Description = studio.Description ?? "",
                Address = studio.Address,
                PricePerHour = studio.PricePerHour,
                ImageUrl = studio.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditStudio(StudioViewModel model, IFormFile? image, bool? removeImage)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(model);

            // Свойства должны точно совпадать с StudioUpdateDto на бэкенде
            var studioData = new
            {
                name = model.Name,
                description = model.Description ?? "",
                address = model.Address,
                pricePerHour = model.PricePerHour,
                imageUrl = model.ImageUrl ?? "",
                removeImage = removeImage ?? false
            };

            var (success, apiError) = await _api.PutWithFileAsync($"Studios/{model.Id}", studioData, image);

            if (apiError != null)
            {
                _notifications.AddError(this, apiError.Message ?? "Ошибка при обновлении студии");
                return View(model);
            }

            _notifications.AddSuccess(this, $"Студия \"{model.Name}\" успешно обновлена");
            return RedirectToAction("Studios");
        }

        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (bookings, error) = await _api.GetAsync<List<Booking>>("Bookings");

            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при загрузке бронирований");
            return View(bookings ?? new List<Booking>());
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (success, error) = await _api.PutAsync($"Bookings/{id}/confirm", new { });

            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при подтверждении");
            else _notifications.AddSuccess(this, "Бронирование подтверждено");

            return RedirectToAction("Bookings");
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (success, error) = await _api.PutAsync($"Bookings/{id}/cancel", new { });

            if (error != null) _notifications.AddError(this, error.Message ?? "Ошибка при отмене");
            else _notifications.AddSuccess(this, "Бронирование отменено");

            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> ExportStudios()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (data, error) = await _api.DownloadFileAsync("Export/studios");
            if (error != null || data == null) return RedirectToAction("Studios");
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Студии_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportBookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (data, error) = await _api.DownloadFileAsync("Export/bookings");
            if (error != null || data == null) return RedirectToAction("Bookings");
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Бронирования_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (data, error) = await _api.DownloadFileAsync("Export/users");
            if (error != null || data == null) return RedirectToAction("Users");
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Пользователи_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportStudioReport(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var (data, error) = await _api.DownloadFileAsync($"Export/studio-report/{id}");
            if (error != null || data == null) return RedirectToAction("Studios");
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Отчет_студия_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}