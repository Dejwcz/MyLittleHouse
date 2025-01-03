using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MujDomecek.Models;
using System.Diagnostics;
using System.Globalization;

namespace MujDomecek.Controllers;

public class HomeController(ILogger<HomeController> _logger, IStringLocalizer<SharedResource> _localizer) : Controller {

    public IActionResult Index() {
        // Testing localization
        //Console.WriteLine(typeof(SharedResource).FullName);
        //Console.WriteLine("Culture: " + CultureInfo.CurrentCulture +
        //      ", UICulture: " + CultureInfo.CurrentUICulture);
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
