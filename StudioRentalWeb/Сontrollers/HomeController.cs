using Microsoft.AspNetCore.Mvc;

namespace StudioRentalWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}