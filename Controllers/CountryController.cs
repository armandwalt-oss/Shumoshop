using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Handles the header country-switcher dropdown. Writes the visitor's
    /// chosen country to a long-lived cookie; the middleware reads it back
    /// on every subsequent request.
    /// </summary>
    public class CountryController : Controller
    {
        private readonly ICountryService _countries;

        public CountryController(ICountryService countries)
        {
            _countries = countries;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(string code, string? returnUrl)
        {
            // Validate the code corresponds to a real active country before
            // committing — protects the cookie from junk values.
            var country = await _countries.GetCountryAsync(code);
            if (country is null)
            {
                TempData["ErrorMessage"] = "That country is not currently supported.";
            }
            else
            {
                Response.Cookies.Append(
                    CountryKeys.CountryCookieName,
                    country.Code,
                    new CookieOptions
                    {
                        IsEssential = true,
                        HttpOnly = false,                          // readable by client JS for UI hints
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps
                    });
            }

            // Bounce back to wherever the user came from; default to home.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}
