using Microsoft.AspNetCore.Mvc;

namespace NHSP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
