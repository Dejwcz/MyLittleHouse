using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace MujDomecek.Controllers;

public class UnitsController(UnitService _service, UserManager<AppUser> _userManager) : Controller
{
    [Authorize]

    [HttpGet]
    public Task<IActionResult> Index()
    {
        return Task.FromResult<IActionResult>(View(User.IsInRole("DungeonMaster") ?
            _service.GetAllUnits() : _service.GetAllUnitsByUserId(_userManager.GetUserId(User))));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var unit = await _service.FindByIdAsync(id);
        if (unit == null)
        {
            return NotFound();
        }
        if (!_service.GetAllUnitsIdsByUserId(_userManager.GetUserId(User)).Contains(id) && !User.IsInRole("DungeonMaster")) {
            return Unauthorized();
        }
            return View(unit);
    }

    [HttpGet]
    public IActionResult Create() {
        ViewBag.UnitTypes = _service.GetEnumUnitTypes();
        ViewBag.PropertyId = _service.GetPropertyByUserId(_userManager.GetUserId(User));
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        //foreach (var item in ViewBag.Units)
        //{
        //    Console.WriteLine(item.Text);
        //}
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync(UnitDto unitDto)
    {
        if (ModelState.IsValid)
        {
            await _service.CreateUnitAsync(unitDto, _userManager.GetUserId(User));
            return RedirectToAction(nameof(Index));
        }
        ViewBag.UnitTypes = _service.GetEnumUnitTypes();
        ViewBag.PropertyId = _service.GetPropertyByUserId(_userManager.GetUserId(User));
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        return View(unitDto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {

        var unit = await _service.FindByIdAsync(id);
        if (unit == null)
        {
            return NotFound();
        }
        if (!_service.GetAllUnitsIdsByUserId(_userManager.GetUserId(User)).Contains(id))
        {
            return Unauthorized();
        }

        ViewBag.UnitTypes = _service.GetEnumUnitTypes();
        ViewBag.PropertyId = _service.GetPropertyByUserId(_userManager.GetUserId(User));
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));

        return View(unit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitDto unitDto)
    {
        if (id != unitDto.Id)
        {
            return NotFound();
        }

        if (!_service.GetAllUnitsIdsByUserId(_userManager.GetUserId(User)).Contains(id)) {
            return Unauthorized();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _service.EditUnitAsync(unitDto, id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_service.UnitExists(unitDto.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(unitDto);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id) {

        var unitDto = await _service.FindByIdAsync(id);
        ViewBag.SubUnits = _service.GetSubordinateUnitsNames(id);
        if (unitDto == null) {
            return NotFound();
        }
        if (!_service.GetAllUnitsIdsByUserId(_userManager.GetUserId(User)).Contains(id)) {
            return Unauthorized();
        }

        return View(unitDto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _service.DeleteUnitAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> FullViewAsync(int id) {
        if (!_service.GetAllUnitsIdsByUserId(_userManager.GetUserId(User)).Contains(id) && !User.IsInRole("DungeonMaster")) {
           return Unauthorized();
        }
        var property = await _service.FindByIdAsync<UnitFullViewDto>(id);
        if (property == null) {
            return NotFound();
        }
        return View(property);
    }
}
