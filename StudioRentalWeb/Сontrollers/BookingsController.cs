using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApiService _api;

        public BookingsController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> MyBookings()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            var bookings = await _api.GetAsync<List<Booking>>($"Bookings/user/{userId}");

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View(bookings ?? new List<Booking>());
        }

        [HttpPost]
        public async Task<IActionResult> Create(int studioId, DateTime startTime, DateTime endTime)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            var booking = new
            {
                CustomerId = userId,
                StudioId = studioId,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            var result = await _api.PostAsync<Booking>("Bookings", booking);

            if (result != null)
            {
                TempData["Success"] = "Бронирование создано успешно";
            }
            else
            {
                TempData["Error"] = "Ошибка при создании бронирования";
            }

            return RedirectToAction("MyBookings", "Bookings");
        }

        public async Task<IActionResult> Cancel(int id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _api.PutAsync($"Bookings/{id}/cancel", new { });

            if (result)
            {
                TempData["Success"] = "Бронирование отменено";
            }
            else
            {
                TempData["Error"] = "Ошибка при отмене бронирования";
            }

            return RedirectToAction("MyBookings", "Bookings");
        }
    }
}