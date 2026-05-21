using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class TermsController : Controller
    {
        // GET: Terms/Index - Terms & Conditions Page
        public IActionResult Index()
        {
            return View();
        }
    }
}