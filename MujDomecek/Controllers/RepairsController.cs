using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MujDomecek.Controllers;

public class RepairsController(ApplicationDbContext _context, RepairService _service, UserManager<AppUser> _userManager) : Controller
{
    [Authorize]

    [HttpGet]
    public Task<IActionResult> Index()
    {
        return Task.FromResult<IActionResult>(View(User.IsInRole("DungeonMaster") ?
            _service.GetAllRepairs() : _service.GetAllRepairsByUserId(_userManager.GetUserId(User))));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {

        if (!_service.GetAllRepairsIdsByUserId(_userManager.GetUserId(User)).Contains(id) && !User.IsInRole("DungeonMaster"))
        {
            return Unauthorized();
        }
        var repair = await _service.FindByIdAsync<RepairDetailViewDto>(id);
        if (repair == null)
        {
            return NotFound();
        }

        return View(repair);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAsync(RepairDto repairDto, IFormCollection files)
    {
        if (ModelState.IsValid)
        {
            await _service.CreateRepairAsync(repairDto, files, _userManager.GetUserId(User));
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        return View(repairDto);
    }

    [HttpGet]
    public async Task<IActionResult> EditAsync(int id)
    {
        if (!_service.GetAllRepairsIdsByUserId(_userManager.GetUserId(User)).Contains(id)) {
            return Unauthorized();
        }
        var repair = await _service.FindByIdAsync<RepairDetailViewDto>(id);
        if (repair == null)
        {
            return NotFound();
        }
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        return View(repair);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAsync(int id, RepairDetailViewDto repairDetailViewDto, Dictionary<int, string> descriptions)
    {
        foreach (var description in descriptions)
        {
            Console.WriteLine($"{description.Key} - {description.Value}");
        }
        if (id != repairDetailViewDto.Id)
        {
            return NotFound();
        }

        if (!_service.GetAllRepairsIdsByUserId(_userManager.GetUserId(User)).Contains(id))
        {
            return Unauthorized();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _service.EditRepairAsync(repairDetailViewDto, id, descriptions);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_service.RepairExists(repairDetailViewDto.Id))
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
        ViewBag.Units = _service.GetUnits(_userManager.GetUserId(User));
        return View(repairDetailViewDto);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var repairDto = await _service.FindByIdAsync<RepairDto>(id);
        if (repairDto == null) {
            return NotFound();
        }
        if (!_service.GetAllRepairsIdsByUserId(_userManager.GetUserId(User)).Contains(id)) {
            return Unauthorized();
        }
        return View(repairDto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _service.DeleteRepairAsync(id);
        return RedirectToAction(nameof(Index));
    }

}
