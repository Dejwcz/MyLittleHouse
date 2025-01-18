using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace MujDomecek.Controllers {
    public class AdminController(AdminService _service, IStringLocalizer<SharedResource> _localizer) : Controller {
        [Authorize(Roles = "DungeonMaster")]
        [HttpGet]
        public IActionResult Index() {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CleanupDatabaseAsync() {
            try {
            await _service.CleanUpDatabaseAsync();
            TempData["Success"] = "Database cleaned up successfully.";
            }
            catch (Exception e) {
                TempData["Error"] = "An error occurred while cleaning up the database.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult UsersList() {
            return View();
        }
    }
}
