using Microsoft.AspNetCore.Mvc;

namespace StudioRentalWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Главная страница теперь публичная. Данные о пользователе подставляются, если он вошел.
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.IsLoggedIn = HttpContext.Session.GetString("UserId") != null;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}