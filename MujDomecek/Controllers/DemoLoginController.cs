using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MujDomecek.Areas.Identity.Pages.Account;

namespace MujDomecek.Controllers;
public class DemoLoginController(SignInManager<AppUser> _signInManager, ILogger<LoginModel> _logger, 
    ApplicationDbContext _context, DemoLoginService _service) : Controller  {
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DemoLoginTom() {
        await _signInManager.SignOutAsync();
        var userName = "tom@cat.com";
        var result = await _signInManager.PasswordSignInAsync(userName, "HeavenlyPuss1949", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
            await _service.UpdateLastLogin(userName);
            return RedirectToAction("Index", "Home");
        }
        else {
            _logger.LogWarning("User login failed.");
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DemoLoginJerry() {
        await _signInManager.SignOutAsync();
        var userName = "jerry@mouse.com";
        var result = await _signInManager.PasswordSignInAsync(userName, "MouseTrouble1944", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
            await _service.UpdateLastLogin(userName);
            return RedirectToAction("Index", "Home");
        }
        else {
            _logger.LogWarning("User login failed.");
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DemoLoginAdmin() {
        await _signInManager.SignOutAsync();
        var userName = "info@x213.cz";
        var result = await _signInManager.PasswordSignInAsync(userName, "MickeyMouse1928", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
            await _service.UpdateLastLogin(userName);
            return RedirectToAction("Index", "Home");
        }
        else {
            _logger.LogWarning("User login failed.");
            return RedirectToAction("Index", "Home");
        }
    }
}
