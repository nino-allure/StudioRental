using Microsoft.AspNetCore.Mvc;
using StudioRentalWeb.Models;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _api;
        private readonly NotificationService _notifications;

        public AccountController(ApiService api, NotificationService notifications)
        {
            _api = api;
            _notifications = notifications;
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

            var (response, apiError) = await _api.PostAsync<LoginResponseDto>("Auth/login", new
            {
                email = model.Email,
                password = model.Password
            });

            if (apiError != null)
            {
                _notifications.AddError(this, apiError.Message ?? "Ошибка при входе");
                ModelState.AddModelError("", apiError.Message ?? "Неверный email или пароль");
                return View(model);
            }

            if (response == null)
            {
                _notifications.AddError(this, "Не удалось получить данные пользователя");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", response.UserId.ToString());
            HttpContext.Session.SetString("UserName", response.FullName);
            HttpContext.Session.SetString("UserRole", response.Role);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("JwtToken", response.Token);

            _notifications.AddSuccess(this, $"Добро пожаловать, {response.FullName}!");

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
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _notifications.AddError(this, modelError.ErrorMessage);
                }
                return View(model);
            }

            // ВАЖНО: Имена свойств должны точно совпадать с RegisterRequestDto на бэкенде
            var registerData = new
            {
                email = model.Email,
                password = model.Password,
                fullName = model.FullName,
                phone = model.Phone ?? ""
            };

            var (response, apiError) = await _api.PostAsync<LoginResponseDto>("Auth/register", registerData);

            if (apiError != null)
            {
                _notifications.AddError(this, apiError.Message ?? "Ошибка при регистрации");
                ModelState.AddModelError("", apiError.Message ?? "Email может быть уже занят");
                return View(model);
            }

            if (response == null)
            {
                _notifications.AddError(this, "Не удалось получить данные пользователя");
                return View(model);
            }

            HttpContext.Session.SetString("UserId", response.UserId.ToString());
            HttpContext.Session.SetString("UserName", response.FullName);
            HttpContext.Session.SetString("UserRole", response.Role);
            HttpContext.Session.SetString("UserEmail", response.Email);
            HttpContext.Session.SetString("JwtToken", response.Token);

            _notifications.AddSuccess(this, "Регистрация прошла успешно!");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _notifications.AddInfo(this, "Вы вышли из системы");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
            var (user, error) = await _api.GetAsync<dynamic>($"Users/{userId}");

            if (error != null || user == null)
            {
                _notifications.AddError(this, "Не удалось загрузить данные профиля");
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileViewModel
            {
                FullName = user.fullName ?? user.FullName ?? "",
                Email = user.email ?? user.Email ?? "",
                Phone = user.phone ?? user.Phone
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _notifications.AddError(this, modelError.ErrorMessage);
                }
                return View(model);
            }

            var (success, error) = await _api.PutAsync("Users/profile", new
            {
                fullName = model.FullName,
                email = model.Email,
                phone = model.Phone ?? ""
            });

            if (error != null)
            {
                _notifications.AddError(this, error.Message ?? "Ошибка при обновлении профиля");
                return View(model);
            }

            HttpContext.Session.SetString("UserName", model.FullName);
            _notifications.AddSuccess(this, "Профиль успешно обновлен!");
            return RedirectToAction("Profile");
        }
    }
}