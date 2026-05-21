using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;

namespace WebApplication1.Controllers
{
    public class FindAStoreController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FindAStoreController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FindAStore
        public ActionResult Index()
        {
            var stores = _context.Stores.Where(s => s.IsActive).ToList();
            return View(stores);
        }
    }
}