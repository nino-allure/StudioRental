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
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var (users, usersError) = await _api.GetAsync<List<User>>("Users");
            var (studios, studiosError) = await _api.GetAsync<List<Studio>>("Studios");
            var (bookings, bookingsError) = await _api.GetAsync<List<Booking>>("Bookings");

            ViewBag.UsersCount = users?.Count ?? 0;
            ViewBag.StudiosCount = studios?.Count ?? 0;
            ViewBag.BookingsCount = bookings?.Count ?? 0;

            return View();
        }

        // GET: Список пользователей
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (users, error) = await _api.GetAsync<List<User>>("Users");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке пользователей");
                return View(new List<User>());
            }

            return View(users ?? new List<User>());
        }

        // DELETE: Удаление пользователя
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (success, error) = await _api.DeleteAsync($"Users/{id}");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при удалении пользователя");
            }
            else
            {
                _notifications.AddSuccess(this, "Пользователь удален");
            }

            return RedirectToAction("Users");
        }

        // GET: Список студий
        public async Task<IActionResult> Studios()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (studios, error) = await _api.GetAsync<List<Studio>>("Studios");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке студий");
                return View(new List<Studio>());
            }

            return View(studios ?? new List<Studio>());
        }

        // GET: Форма добавления студии
        [HttpGet]
        public IActionResult CreateStudio()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        // POST: Добавление студии
        [HttpPost]
        [Route("Admin/CreateStudio")]
        public async Task<IActionResult> CreateStudio(StudioViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studioData = new
            {
                name = model.Name,
                description = model.Description,
                address = model.Address,
                pricePerHour = model.PricePerHour,
                imageUrl = model.ImageUrl ?? "/img/gear.jpg",
                isApproved = true
            };

            var (result, error) = await _api.PostAsync<Studio>("Studios", studioData);

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при создании студии");
                return View(model);
            }

            _notifications.AddSuccess(this, $"Студия \"{model.Name}\" успешно создана");
            return RedirectToAction("Studios");
        }

        // POST: Удаление студии
        [HttpPost]
        public async Task<IActionResult> DeleteStudio(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (success, error) = await _api.DeleteAsync($"Studios/{id}");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при удалении студии");
            }
            else
            {
                _notifications.AddSuccess(this, "Студия удалена");
            }

            return RedirectToAction("Studios");
        }

        // GET: Редактирование студии
        [HttpGet]
        [Route("Admin/EditStudio/{id}")]
        public async Task<IActionResult> EditStudio(int id)
        {
            Console.WriteLine($"=== EDIT STUDIO GET CALLED === ID: {id}");

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
        [Route("Admin/EditStudio")]
        public async Task<IActionResult> EditStudio(StudioViewModel model)
        {
            Console.WriteLine($"=== EDIT STUDIO POST CALLED === ID: {model.Id}");
            Console.WriteLine($"Name: {model.Name}");
            Console.WriteLine($"Address: {model.Address}");
            Console.WriteLine($"Price: {model.PricePerHour}");

            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model invalid!");
                return View(model);
            }

            var studioData = new
            {
                name = model.Name,
                description = model.Description ?? "",
                address = model.Address,
                pricePerHour = model.PricePerHour,
                imageUrl = model.ImageUrl ?? "/img/gear.jpg"
            };

            var (success, apiError) = await _api.PutAsync($"Studios/{model.Id}", studioData);

            if (apiError != null)
            {
                Console.WriteLine($"API Error: {apiError.Message}");
                _notifications.AddError(this, apiError.Message ?? "Ошибка при обновлении студии");
                return View(model);
            }

            Console.WriteLine("Studio updated successfully!");
            _notifications.AddSuccess(this, $"Студия \"{model.Name}\" успешно обновлена");
            return RedirectToAction("Studios");
        }

        // GET: Список бронирований
        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (bookings, error) = await _api.GetAsync<List<Booking>>("Bookings");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке бронирований");
                return View(new List<Booking>());
            }

            return View(bookings ?? new List<Booking>());
        }

        // POST: Подтверждение бронирования
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (success, error) = await _api.PutAsync($"Bookings/{id}/confirm", new { });

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при подтверждении бронирования");
            }
            else
            {
                _notifications.AddSuccess(this, "Бронирование подтверждено");
            }

            return RedirectToAction("Bookings");
        }

        // POST: Отмена бронирования (админ)
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (success, error) = await _api.PutAsync($"Bookings/{id}/cancel", new { });

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при отмене бронирования");
            }
            else
            {
                _notifications.AddSuccess(this, "Бронирование отменено");
            }

            return RedirectToAction("Bookings");
        }
        public async Task<IActionResult> ExportStudios()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (data, error) = await _api.DownloadFileAsync("Export/studios");

            if (error != null || data == null)
            {
                _notifications.AddError(this, error?.Message ?? "Ошибка при экспорте");
                return RedirectToAction("Studios");
            }

            var fileName = $"Студии_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportBookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (data, error) = await _api.DownloadFileAsync("Export/bookings");

            if (error != null || data == null)
            {
                _notifications.AddError(this, error?.Message ?? "Ошибка при экспорте");
                return RedirectToAction("Bookings");
            }

            var fileName = $"Бронирования_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (data, error) = await _api.DownloadFileAsync("Export/users");

            if (error != null || data == null)
            {
                _notifications.AddError(this, error?.Message ?? "Ошибка при экспорте");
                return RedirectToAction("Users");
            }

            var fileName = $"Пользователи_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> ExportStudioReport(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var (data, error) = await _api.DownloadFileAsync($"Export/studio-report/{id}");

            if (error != null || data == null)
            {
                _notifications.AddError(this, error?.Message ?? "Ошибка при экспорте отчета");
                return RedirectToAction("Studios");
            }

            var fileName = $"Отчет_по_студии_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Logs()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(string? level, string? category, DateTime? from, DateTime? to, int page = 1, int pageSize = 50)
        {
            if (!IsAdmin()) return Unauthorized();

            var (data, error) = await _api.GetAsync<dynamic>($"Logs?level={level}&category={category}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&page={page}&pageSize={pageSize}");

            if (error != null)
                return Json(new { logs = new List<object>(), totalCount = 0 });

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetLogsStats()
        {
            if (!IsAdmin()) return Unauthorized();

            var (data, error) = await _api.GetAsync<dynamic>("Logs/stats");

            if (error != null)
                return Json(new { totalLogs = 0, errorsLast24h = 0 });

            return Json(data);
        }
    }
}