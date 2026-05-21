using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class RegisterUserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterUserController> _logger;
        private readonly IEmailService _emailService;

        public RegisterUserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterUserController> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Surname = model.Surname, // ✅ ADDED
                    PhoneNumber = model.PhoneNumber,
                    StreetAddress = model.StreetAddress ?? string.Empty,
                    City = model.City ?? string.Empty,
                    State = model.State ?? string.Empty,
                    PostalCode = model.PostalCode ?? string.Empty
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Automatically assign "User" role to new registrations
                    await _userManager.AddToRoleAsync(user, "User");

                    // Welcome email — best-effort, must not block signup.
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}
