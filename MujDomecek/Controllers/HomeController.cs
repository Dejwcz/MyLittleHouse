using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MujDomecek.Models;
using System.Diagnostics;
using System.Globalization;

namespace MujDomecek.Controllers
{
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public HomeController(ILogger<HomeController> logger, IStringLocalizer<SharedResource> localizer) {
            _logger = logger;
            _localizer = localizer;
        }

        public IActionResult Index() {
            Console.WriteLine(typeof(SharedResource).FullName);
            Console.WriteLine("Culture: " + CultureInfo.CurrentCulture +
                  ", UICulture: " + CultureInfo.CurrentUICulture);
            var asm = typeof(SharedResource).Assembly;
            foreach (var name in asm.GetManifestResourceNames()) {
                Console.WriteLine(name);
            }
            return View();
        }

        public IActionResult Privacy() {
            return View();
        }

        public IActionResult Info() {
            return View();
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl = "/") {
            // Save cookie
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // Redirect back
            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
