using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApiService _api;
        private readonly NotificationService _notifications;

        public BookingsController(ApiService api, NotificationService notifications)
        {
            _api = api;
            _notifications = notifications;
        }

        public async Task<IActionResult> MyBookings()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            var (bookings, error) = await _api.GetAsync<List<Booking>>($"Bookings/user/{userId}");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке бронирований");
                return View(new List<Booking>());
            }

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View(bookings ?? new List<Booking>());
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Добавляем детали ошибок в уведомление
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _notifications.AddError(this, $"Ошибка валидации: {errors}");
                return RedirectToAction("Details", "Studios", new { id = model.StudioId });
            }

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            // ИСПРАВЛЕНО: Используем PascalCase для совместимости с бэкендом
            var bookingPayload = new
            {
                CustomerId = userId,  // Было: customerId
                StudioId = model.StudioId,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            };

            var (result, error) = await _api.PostAsync<Booking>("Bookings", bookingPayload);

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при создании бронирования");
                return RedirectToAction("Details", "Studios", new { id = model.StudioId });
            }

            _notifications.AddSuccess(this, "Бронирование создано успешно и ожидает подтверждения!");
            return RedirectToAction("MyBookings", "Bookings");
        }
        public async Task<IActionResult> Cancel(int id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var (success, error) = await _api.PutAsync($"Bookings/{id}/cancel", new { });
            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при отмене бронирования");
            }
            else
            {
                _notifications.AddSuccess(this, "Бронирование отменено");
            }

            return RedirectToAction("MyBookings", "Bookings");
        }
    }
}