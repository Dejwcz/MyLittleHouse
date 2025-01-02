using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace MujDomecek.Services; 
public class UnitService(ApplicationDbContext _context, IStringLocalizer<SharedResource> _localizer) {

    internal async Task<T> FindByIdAsync<T>(int id) {
        var unit = await _context.Units
                .Include(u => u.Repairs)
                .Include(u => u.ChildUnits)
                .FirstOrDefaultAsync(u => u.Id == id);                ;

        if (unit == null) {
            return default; 
        }

        return ModelToDtoGen<T>(unit);
    }

    private T ModelToDtoGen<T>(Unit unit) {
        if (unit == null) return default;
        if (typeof(T) == typeof(UnitDto)) {
            return (T)(object)ModelToDto(unit);
        }
        else if (typeof(T) == typeof(UnitFullViewDto)) {
            return (T)(object)ModelToFullViewDto(unit);
        }
        throw new InvalidOperationException($"Mapping for {typeof(T).Name} is not defined.");
    }

    private object ModelToFullViewDto(Unit unit) {
        return new UnitFullViewDto {
            Id = unit.Id,
            Name = unit.Name,
            Description = unit.Description,
            UnitType = unit.UnitType,
            PropertyId = unit.PropertyId,
            ParentUnitName = unit.ParentUnitId == null ? null : _context.Units.FirstOrDefault(u => u.Id == unit.ParentUnitId).Name,
            ChildUnitsNames = unit.ChildUnits.Select(x => x.Name).ToList(),
            Repairs = unit.Repairs.Select(r => new RepairDto {
                Id = r.Id,
                Description = r.Description,
                Title = r.Title,
                Cost = r.Cost,
                RepairDate = r.RepairDate,
                UnitId = r.UnitId,
            }).ToList() ?? new List<RepairDto>(),
        };
    }

    internal IEnumerable<UnitDto> GetAllUnits() {
        var allUnits = _context.Units;
        var UnitsDtos = new List<UnitDto>();
        foreach (var units in allUnits) {
            UnitsDtos.Add(ModelToDto(units));
        }
        return UnitsDtos;
    }

    private UnitDto ModelToDto(Unit units) {
        return new UnitDto {
            Id = units.Id,
            Name = units.Name,
            Description = units.Description,
            UnitType = units.UnitType,
            PropertyId = units.PropertyId,
            ParentUnitId = units.ParentUnitId,
            ParentUnitName = units.ParentUnitId == null ? null : _context.Units.FirstOrDefault(u => u.Id == units.ParentUnitId).Name,
        };
    }

    internal IEnumerable<UnitDto> GetAllUnitsByUserId(string v) {
        //var allUnits = _context.Units.Join(_context.Properties, u => u.PropertyId, p => p.Id, (u, p) => new { u, p })
        //    .Where(x => x.p.UserId == v)
        //    .Select(x => x.u);
        //var UnitsDtos = new List<UnitDto>();
        //foreach (var units in allUnits) {
        //    UnitsDtos.Add(ModelToDto(units));
        //}
        //return UnitsDtos;

        //Keep it simple
        return _context.Properties.Where(x => x.UserId == v).SelectMany(x => x.Units).Select(ModelToDto).ToList();
    }

    internal IEnumerable<int> GetAllUnitsIdsByUserId(string v) {
        //var allUnits = _context.Units.Join(_context.Properties, u => u.PropertyId, p => p.Id, (u, p) => new { u, p })
        //    .Where(x => x.p.UserId == v)
        //    .Select(x => x.u);
        //var UnitsIds = new List<int>();
        //foreach (var units in allUnits) {
        //    UnitsIds.Add(units.Id);
        //}
        //return UnitsIds;

        //Keep it simple 
        return _context.Properties.Where(x => x.UserId == v).SelectMany(x => x.Units).Select(u => u.Id).ToList();
    }

    internal IEnumerable<SelectListItem> GetUnits(string v) {
        var units = GetAllUnitsByUserId(v);
        return units.Select(u => new SelectListItem {
            Value = u.Id.ToString(),
            Text = _localizer[u.UnitType.ToString()] + " - " + u.Name +  (u.Description == null ? 
            "" : " (" + u.Description.Substring(0, u.Description.Length < 30 ? u.Description.Length : 30) + ")"),
        }).Prepend(new SelectListItem {
            Value = "", // Empty value
            Text = _localizer["None"] // Text shown in the dropdown
        });
    }

    internal IEnumerable<SelectListItem> GetEnumUnitTypes() {
        return Enum.GetValues(typeof(UnitType))
                            .Cast<UnitType>()
                            .Select(ut => new SelectListItem {
                                Value = ut.ToString(),
                                Text = _localizer[ut.ToString()]
                            })
                            .ToList();
    }

    internal IEnumerable<SelectListItem> GetPropertyByUserId(string? v) {
        return _context.Properties
            .Where(x => x.UserId == v)
            .Select(p => new SelectListItem {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToList();
    }

    internal async Task CreateUnitAsync(UnitDto unitDto, string? v) {
        await _context.Units.AddAsync(DtoToModel(unitDto));
        await _context.SaveChangesAsync();
    }

    private Unit DtoToModel(UnitDto unitDto) {
        return new Unit {
            Id = unitDto.Id,
            Name = unitDto.Name,
            Description = unitDto.Description,
            UnitType = unitDto.UnitType,
            PropertyId = unitDto.PropertyId,
            ParentUnitId = unitDto.ParentUnitId,
        };
    }

    internal async Task<UnitDto> FindByIdAsync(int id) {
        return ModelToDto(await _context.Units.FirstOrDefaultAsync(m => m.Id == id));
    }

    internal async Task EditUnitAsync(UnitDto unitDto, int id) {
        var unitToEdit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id);
        if (unitToEdit != null) {
            unitToEdit.Name = unitDto.Name;
            unitToEdit.Description = unitDto.Description;
            unitToEdit.UnitType = unitDto.UnitType;
            unitToEdit.PropertyId = unitDto.PropertyId;
            unitToEdit.ParentUnitId = unitDto.ParentUnitId;
        }
        await _context.SaveChangesAsync();
    }

    internal async Task DeleteUnitAsync(int id) {
        var unitToDelete = _context.Units.FirstOrDefault(u => u.Id == id);
        if (unitToDelete != null) {
            _context.Units.Remove(unitToDelete);
        }
        await _context.SaveChangesAsync();
    }

    internal bool UnitExists(int id) {
        return _context.Units.Any(e => e.Id == id);
    }
}
