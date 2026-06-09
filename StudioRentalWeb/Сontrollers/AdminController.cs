using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;      // ← ЭТА СТРОКА ВАЖНА!
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _api;

        public AdminController(ApiService api)
        {
            _api = api;
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

            var users = await _api.GetAsync<List<User>>("Users");
            var studios = await _api.GetAsync<List<Studio>>("Studios");
            var bookings = await _api.GetAsync<List<Booking>>("Bookings");

            ViewBag.UsersCount = users?.Count ?? 0;
            ViewBag.StudiosCount = studios?.Count ?? 0;
            ViewBag.BookingsCount = bookings?.Count ?? 0;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var users = await _api.GetAsync<List<User>>("Users");
            return View(users ?? new List<User>());
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            await _api.DeleteAsync($"Users/{id}");
            TempData["Success"] = "Пользователь удален";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Studios()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var studios = await _api.GetAsync<List<Studio>>("Studios");
            return View(studios ?? new List<Studio>());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudio(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            await _api.DeleteAsync($"Studios/{id}");
            TempData["Success"] = "Студия удалена";
            return RedirectToAction("Studios");
        }

        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var bookings = await _api.GetAsync<List<Booking>>("Bookings");
            return View(bookings ?? new List<Booking>());
        }

        public async Task<IActionResult> ConfirmBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            await _api.PutAsync($"Bookings/{id}/confirm", new { });
            TempData["Success"] = "Бронирование подтверждено";
            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            await _api.PutAsync($"Bookings/{id}/cancel", new { });
            TempData["Success"] = "Бронирование отменено";
            return RedirectToAction("Bookings");
        }
    }
}