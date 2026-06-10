using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

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

            var response = await _api.PostAsync<LoginResponseDto>("Auth/login", new
            {
                email = model.Email,
                password = model.Password
            });

            if (response == null)
            {
                ModelState.AddModelError("", "Неверный email или пароль");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", response.UserId.ToString());
            HttpContext.Session.SetString("UserName", response.FullName);
            HttpContext.Session.SetString("UserRole", response.Role);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("JwtToken", response.Token);

            if (response.Role == "Admin")
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
            Console.WriteLine($"=== REGISTER VIA WEB ===");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"FullName: {model.FullName}");
            Console.WriteLine($"Password: {model.Password}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Model Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            var response = await _api.PostAsync<LoginResponseDto>("Auth/register", new
            {
                email = model.Email,
                password = model.Password,
                fullName = model.FullName,
                phone = model.Phone ?? ""
            });

            if (response == null)
            {
                ModelState.AddModelError("", "Ошибка при регистрации. Email может быть уже занят.");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", response.UserId.ToString());
            HttpContext.Session.SetString("UserName", response.FullName);
            HttpContext.Session.SetString("UserRole", response.Role);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("JwtToken", response.Token);

            Console.WriteLine($"Registration successful! UserId: {response.UserId}");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}