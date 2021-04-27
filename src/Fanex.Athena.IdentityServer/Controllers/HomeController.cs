using Microsoft.AspNetCore.Mvc;

namespace Fanex.Athena.IdentityServerAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}