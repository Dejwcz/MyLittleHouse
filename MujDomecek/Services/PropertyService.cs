namespace MujDomecek.Services;
[Authorize]
public class PropertyService(ApplicationDbContext _context) {

    internal async Task<T> FindByIdAsync<T>(int id) {
        var property = await _context.Properties
                .Include(p => p.Units)
                .ThenInclude(u => u.Repairs)
                .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) {
            return default; 
        }

        return ModelToDtoGen<T>(property);
    }

    private T ModelToDtoGen<T>(Property property) {
        if (property == null) return default;
        if (typeof(T) == typeof(PropertyDto)) {
            return (T)(object)ModelToDto(property);
        }
        else if (typeof(T) == typeof(PropertyWithoutUserIdDto)) {
            return (T)(object)ModelToDtoWithoutUserId(property);
        }
        else if (typeof(T) == typeof(PropertyFullViewDto)) {
            return (T)(object)ModelToFullViewDto(property);
        }
        else if (typeof(T) == typeof(PropertyDetailsDto)) {
            return (T)(object)ModelToDetailsDto(property);
        }
        throw new InvalidOperationException($"Mapping for {typeof(T).Name} is not defined.");
    }

    private PropertyDto ModelToDto(Property properties) {
        return new PropertyDto {
            Id = properties.Id,
            Name = properties.Name,
            Description = properties.Description,
            UserId = properties.UserId,
        };
    }
    private PropertyDetailsDto ModelToDetailsDto(Property property) {
        return new PropertyDetailsDto {
            Id = property.Id,
            Name = property.Name,
            Description = property.Description,
            CreatedAt = property.CreatedAt,
            UpdatedAt = property.UpdatedAt,
            UnitNames = property.Units.Select(u => u.Name).ToList(),
            RepaisCount = property.Units.SelectMany(u => u.Repairs).Count(),
            RepaisCost = property.Units.SelectMany(u => u.Repairs).Sum(r => r.Cost ?? 0),
        };
    }
    private PropertyFullViewDto ModelToFullViewDto(Property property) {
        var propertyFull = new PropertyFullViewDto {
            Id = property.Id,
            Name = property.Name,
            Description = property.Description,
            UserId = property.UserId,
            Units = property.Units.Select(unit => new UnitFullViewDto {
                Id = unit.Id,
                Name = unit.Name,
                Description = unit.Description,
                UnitType = unit.UnitType,
                Repairs = unit.Repairs?.Select(repair => new RepairDto {
                    Id = repair.Id,
                    Title = repair.Title,
                    Description = repair.Description,
                    Cost = repair.Cost,
                    RepairDate = repair.RepairDate
                }).ToList() ?? new List<RepairDto>(),
                ParentUnitName = unit.ParentUnitId == null ? null : _context.Units.FirstOrDefault(u => u.Id == unit.ParentUnitId).Name,
                PropertyId = unit.PropertyId,
            }).ToList() ?? new List<UnitFullViewDto> ()
        };
        return propertyFull;
    }
    private PropertyWithoutUserIdDto ModelToDtoWithoutUserId(Property properties) {
        return new PropertyWithoutUserIdDto {
            Id = properties.Id,
            Name = properties.Name,
            Description = properties.Description,
        };
    }

    private Property DtoToModel(PropertyWithoutUserIdDto propertyDto, string userId) {
        return new Property {
            Id = propertyDto.Id,
            Name = propertyDto.Name,
            Description = propertyDto.Description,
            UserId = userId,
        };
    }

    internal IEnumerable<PropertyDto> GetAllProperties() {
        return _context.Properties.Select(ModelToDtoGen<PropertyDto>);
    }

    internal IEnumerable<PropertyDto> GetAllPropertiesByUserId(string userId) {
        return _context.Properties.Where(x => x.UserId == userId).Select(ModelToDtoGen<PropertyDto>);
    }

    internal async Task<bool> CheckPropertyIdToUserId(String? v, int? id) {
        if (id == null || v == null) return false;
        var prop = await _context.Properties.FirstOrDefaultAsync(x => x.Id == id);
        if (prop != null && prop.UserId == v) return true;
        return false;
    }

    internal async Task CreatePropertyAsync(PropertyWithoutUserIdDto propertyDto, string userId) {
        await _context.Properties.AddAsync(DtoToModel(propertyDto, userId));
        await _context.SaveChangesAsync();
    }

    internal async Task EditPropertyAsync(PropertyWithoutUserIdDto propertyDto, string userId) {
        try {
            var propToEdit = _context.Properties.FirstOrDefault(x => x.Id == propertyDto.Id);
            if (propToEdit != null) {
                propToEdit.Name = propertyDto.Name;
                propToEdit.Description = propertyDto.Description;
                propToEdit.UpdatedAt = DateTime.Now;
            };
            await _context.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }

    internal async Task DeleteAsync(int id) {
        var property = await _context.Properties.FindAsync(id);
        if (property != null) {
            property.IsDeleted = true;
            property.DeletedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
    }
    internal bool PropertyExists(int id) {
        return _context.Properties.Any(x => x.Id == id);
    }  
    }

    //Obsolete
    //private Property DtoToModel(PropertyDto propertyDto) {
    //    return new Property {
    //        Id = propertyDto.Id,
    //        Name = propertyDto.Name,
    //        Description = propertyDto.Description,
    //        UserId = propertyDto.UserId,
    //    };
    //}

    //internal async Task<PropertyDto> FindByIdAsync(int? id) {
    //    if (id == null) return null;
    //    return ModelToDto(await _context.Properties.FirstOrDefaultAsync(m => m.Id == id));
    //}

    //internal async Task<PropertyFullViewDto> FindFullPropertyByIdAsync(int id) {
    //    var property = await _context.Properties.Include(x => x.Units).ThenInclude(u => u.Repairs).FirstOrDefaultAsync(x => x.Id == id);
    //    return ModelToFullViewDto(property);
    //}

    //internal async Task<PropertyWithoutUserIdDto> FindByIdWithoutUserIdAsync(int? id) {
    //    if (id == null) return null;
    //    return ModelToDtoWithoutUserId(await _context.Properties.FirstOrDefaultAsync(m => m.Id == id));
    //}