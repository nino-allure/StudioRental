using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class StudiosController : Controller
    {
        private readonly ApiService _api;

        public StudiosController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var studios = await _api.GetAsync<List<Studio>>("Studios");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View(studios ?? new List<Studio>());
        }

        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var studio = await _api.GetAsync<Studio>($"Studios/{id}");
            if (studio == null)
            {
                return NotFound();
            }

            ViewBag.UserId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            return View(studio);
        }
    }
}