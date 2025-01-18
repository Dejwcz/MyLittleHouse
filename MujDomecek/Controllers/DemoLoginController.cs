using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MujDomecek.Areas.Identity.Pages.Account;

namespace MujDomecek.Controllers; 
public class DemoLoginController(SignInManager<AppUser> _signInManager, ILogger<LoginModel> _logger) : Controller  {
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DemoLoginTom() {
        await _signInManager.SignOutAsync();
        var result = await _signInManager.PasswordSignInAsync("tom@cat.com", "HeavenlyPuss1949", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
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
        var result = await _signInManager.PasswordSignInAsync("jerry@mouse.com", "MouseTrouble1944", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
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
        var result = await _signInManager.PasswordSignInAsync("info@x213.cz", "MickeyMouse1928", false, lockoutOnFailure: false);
        if (result.Succeeded) {
            _logger.LogInformation("User logged in.");
            return RedirectToAction("Index", "Home");
        }
        else {
            _logger.LogWarning("User login failed.");
            return RedirectToAction("Index", "Home");
        }
    }
}
