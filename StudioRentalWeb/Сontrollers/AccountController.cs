using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;
using System.Text.Json;

namespace StudioRentalWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var users = await _api.GetAsync<List<User>>("Users");
            var user = users?.FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Неверный email или пароль");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var users = await _api.GetAsync<List<User>>("Users");
            if (users?.Any(u => u.Email == model.Email) == true)
            {
                ModelState.AddModelError("Email", "Email уже используется");
                return View(model);
            }

            var newUser = new
            {
                Email = model.Email,
                Password = model.Password,
                FullName = model.FullName,
                Phone = model.Phone,
                Role = "User",
                CreatedAt = DateTime.Now
            };

            var created = await _api.PostAsync<User>("Users/register", newUser);

            if (created != null)
            {
                HttpContext.Session.SetString("UserId", created.Id.ToString());
                HttpContext.Session.SetString("UserName", created.FullName);
                HttpContext.Session.SetString("UserRole", created.Role);
                HttpContext.Session.SetString("UserEmail", created.Email);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Ошибка при регистрации");
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}