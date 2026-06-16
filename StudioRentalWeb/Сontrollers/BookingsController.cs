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
                _notifications.AddError(this, "Некорректные данные бронирования");
                return RedirectToAction("Details", "Studios", new { id = model.StudioId });
            }

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            // Отправляем только необходимые данные. Status и CreatedAt рассчитаются на сервере.
            var bookingPayload = new
            {
                customerId = userId,
                studioId = model.StudioId,
                startTime = model.StartTime,
                endTime = model.EndTime
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