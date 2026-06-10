using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class StudiosController : Controller
    {
        private readonly ApiService _api;
        private readonly NotificationService _notifications;

        public StudiosController(ApiService api, NotificationService notifications)
        {
            _api = api;
            _notifications = notifications;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var (studios, error) = await _api.GetAsync<List<Studio>>("Studios");

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке студий");
                return View(new List<Studio>());
            }

            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View(studios ?? new List<Studio>());
        }

        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var (studio, error) = await _api.GetAsync<Studio>($"Studios/{id}");

            if (error != null)
            {
                if (error.StatusCode == 404)
                {
                    _notifications.AddError(this, "Студия не найдена");
                    return RedirectToAction("Index");
                }
                _notifications.AddError(this, error.Message ?? "Ошибка при загрузке студии");
                return RedirectToAction("Index");
            }

            if (studio == null)
            {
                _notifications.AddError(this, "Студия не найдена");
                return RedirectToAction("Index");
            }

            ViewBag.UserId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            return View(studio);
        }
    }
}