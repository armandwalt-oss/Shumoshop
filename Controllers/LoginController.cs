using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.Services;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class LoginController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public LoginController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginController> logger,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        // GET: /Login/Index  → serves the view
        [HttpGet]
        public IActionResult Index()
            => View("~/Views/User/Login/Login.cshtml");

        // POST: /Login/Login  → handles sign-in
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/User/Login/Login.cshtml", model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View("~/Views/User/Login/Login.cshtml", model);
            }

            // FIXED: Preserve the cart session ID BEFORE login (before session regeneration)
            var guestSessionId = HttpContext.Request.Cookies["CartSessionId"];

            // ADD DEBUG LOGGING
            _logger.LogInformation($"🔍 BEFORE LOGIN - Cookie CartSessionId: {guestSessionId ?? "NULL"}");

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // ADD DEBUG LOGGING
                _logger.LogInformation($"🔍 AFTER LOGIN - About to merge cart for user {user.Id} with session {guestSessionId ?? "NULL"}");

                // FIXED: Merge guest cart to user cart AFTER successful login
                await MergeGuestCartToUserAsync(user.Id, guestSessionId);

                return LocalRedirect(model.ReturnUrl ?? Url.Content("~/"));
            }

            if (result.RequiresTwoFactor)
                return RedirectToAction("LoginWith2fa", new { model.ReturnUrl, model.RememberMe });

            if (result.IsLockedOut)
                return View("Lockout");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View("~/Views/User/Login/Login.cshtml", model);
        }

        // FIXED: Helper method to merge guest cart to user cart
        private async Task MergeGuestCartToUserAsync(string userId, string guestSessionId)
        {
            if (string.IsNullOrEmpty(guestSessionId))
            {
                _logger.LogInformation("No guest session ID to merge");
                return;
            }

            try
            {
                _logger.LogInformation($"🔍 Starting merge - UserId: {userId}, SessionId: {guestSessionId}");

                // Find guest cart
                var guestCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.SessionId == guestSessionId);

                if (guestCart == null)
                {
                    _logger.LogInformation($"No guest cart found with SessionId: {guestSessionId}");
                    return;
                }

                if (!guestCart.CartItems.Any())
                {
                    _logger.LogInformation($"Guest cart {guestCart.Id} is empty, removing it");
                    _context.Carts.Remove(guestCart);
                    await _context.SaveChangesAsync();
                    return;
                }

                _logger.LogInformation($"Found guest cart {guestCart.Id} with {guestCart.CartItems.Count} items");

                // Find or create user cart
                var userCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (userCart == null)
                {
                    _logger.LogInformation($"No user cart exists, converting guest cart {guestCart.Id} to user cart");

                    // No user cart exists, convert guest cart to user cart
                    guestCart.UserId = userId;
                    guestCart.SessionId = null; // Remove session ID
                    guestCart.LastModifiedDate = DateTime.Now;
                }
                else
                {
                    _logger.LogInformation($"User cart {userCart.Id} exists with {userCart.CartItems.Count} items, merging...");

                    // User cart exists, merge items
                    foreach (var guestItem in guestCart.CartItems.ToList())
                    {
                        var existingItem = userCart.CartItems
                            .FirstOrDefault(ci => ci.ProductId == guestItem.ProductId);

                        if (existingItem != null)
                        {
                            _logger.LogInformation($"Product {guestItem.ProductId} exists in both carts, adding quantities: {existingItem.Quantity} + {guestItem.Quantity}");

                            // Update quantity of existing item
                            existingItem.Quantity += guestItem.Quantity;
                            existingItem.AddedDate = DateTime.Now;
                        }
                        else
                        {
                            _logger.LogInformation($"Product {guestItem.ProductId} only in guest cart, moving to user cart");

                            // Move item to user cart
                            guestItem.CartId = userCart.Id;
                        }
                    }

                    userCart.LastModifiedDate = DateTime.Now;

                    // Remove guest cart
                    _context.Carts.Remove(guestCart);
                    _logger.LogInformation($"Removed guest cart {guestCart.Id}");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Successfully merged guest cart to user cart for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error merging guest cart to user cart for user {UserId}", userId);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            // Redirect somewhere sensible after logout
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // GET: Login/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/User/Login/ForgotPassword.cshtml");
        }

        // POST: Login/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/User/Login/ForgotPassword.cshtml", model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Don't reveal that the user doesn't exist (security best practice)
            if (user == null || user.IsDeleted)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Build the reset link. Use Url.Action so the route is correct
            // wherever the app is hosted.
            var resetLink = Url.Action(
                action: "ResetPassword",
                controller: "Login",
                values: new { email = user.Email, token },
                protocol: Request.Scheme,
                host: Request.Host.Value);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user, resetLink!);
                _logger.LogInformation("Password reset email queued for {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Don't leak the failure to the user — the confirmation page is
                // the same regardless of whether the email actually sent.
                _logger.LogError(ex, "Failed to send password reset email for {Email}", user.Email);
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET: Login/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View("~/Views/User/Login/ForgotPasswordConfirmation.cshtml");
        }

        // GET: Login/ResetPassword?email=...&token=...
        // Reached from the link in the password-reset email.
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "The password reset link is missing required information.");
                return View("~/Views/User/Login/ResetPassword.cshtml", new ResetPasswordViewModel());
            }

            return View("~/Views/User/Login/ResetPassword.cshtml",
                new ResetPasswordViewModel { Email = email, Token = token });
        }

        // POST: Login/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/User/Login/ResetPassword.cshtml", model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Same "don't reveal" rule — if the user doesn't exist, still
            // pretend the reset succeeded so we don't enumerate accounts.
            if (user is null || user.IsDeleted)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset succeeded for {Email}", user.Email);
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View("~/Views/User/Login/ResetPassword.cshtml", model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View("~/Views/User/Login/ResetPasswordConfirmation.cshtml");
        }
    }

    public class ResetPasswordViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string Token { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Confirm new password")]
        [System.ComponentModel.DataAnnotations.Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}