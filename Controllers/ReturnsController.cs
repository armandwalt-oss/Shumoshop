using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class ReturnsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}