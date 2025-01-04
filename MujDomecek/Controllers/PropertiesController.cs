using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MujDomecek.Controllers; 
[Authorize]
public class PropertiesController(UserManager<AppUser> _userManager, PropertyService _service) : Controller {

    [HttpGet]
    public IActionResult Index() {
        ViewBag.UserId = _userManager.GetUserId(User);
        return View(User.IsInRole("DungeonMaster") ?
            _service.GetAllProperties() : _service.GetAllPropertiesByUserId(_userManager.GetUserId(User)));
    }

    [HttpGet]
    public async Task<IActionResult> DetailsAsync(int? id) {
        if (id == null) {
            return NotFound();
        }

        var property = await _service.FindByIdAsync<PropertyDetailsDto>(id.Value);
        if (!await _service.CheckPropertyIdToUserId(_userManager.GetUserId(User), id) && !User.IsInRole("DungeonMaster")) { 
            return Unauthorized();}

        if (property == null) {
            return NotFound();
        }
        return View(property);
    }

    [HttpGet]
    public IActionResult Create() {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync(PropertyWithoutUserIdDto propertyDto) {
        await _service.CreatePropertyAsync(propertyDto, _userManager.GetUserId(User));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditAsync(int? id) {
        if (id == null) {
            return NotFound();
        }
        if (!await _service.CheckPropertyIdToUserId(_userManager.GetUserId(User), id)) {
            return Unauthorized();
        }
        PropertyWithoutUserIdDto propertyDto = await _service.FindByIdAsync<PropertyWithoutUserIdDto>(id.Value);
        if (propertyDto == null) {
            return NotFound();
        }
        return View(propertyDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAsync(int id, PropertyWithoutUserIdDto propertyDto ) {
        var userId = _userManager.GetUserId(User);  
        if (!await _service.CheckPropertyIdToUserId(userId, id)) {
            return Unauthorized();
        }
        if (id != propertyDto.Id) {
            return NotFound();
        }

        if (ModelState.IsValid) {
            try {
                await _service.EditPropertyAsync(propertyDto, userId);
            }
            catch (DbUpdateConcurrencyException) {
                if (!_service.PropertyExists(propertyDto.Id)) {
                    return NotFound();
                }
                else {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteAsync(int id) {
        if (!await _service.CheckPropertyIdToUserId(_userManager.GetUserId(User), id)) {
            return Unauthorized();
        }
        var propertyDto = await _service.FindByIdAsync<PropertyWithoutUserIdDto>(id);
        if (propertyDto == null) {
            return NotFound();
        }
        return View(propertyDto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
        await _service.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> FullViewAsync(int id) {
        if (!await _service.CheckPropertyIdToUserId(_userManager.GetUserId(User), id) && !User.IsInRole("DungeonMaster")) {
            return Unauthorized();
        }
        var property = await _service.FindByIdAsync<PropertyFullViewDto>(id);
        if (property == null) {
            return NotFound();
        }
        return View(property);
    }

}
