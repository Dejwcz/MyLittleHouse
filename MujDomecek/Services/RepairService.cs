using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace MujDomecek.Services;
public class RepairService(ApplicationDbContext _context, IStringLocalizer<SharedResource> _localizer) {
    internal IEnumerable<RepairDto> GetAllRepairs() {
        return _context.Repairs.Select(ModelToDtoGen<RepairDto>).ToList();
    }

    internal T ModelToDtoGen<T>(Repair repair) {
        if (repair == null) return default;
        if (typeof(T) == typeof(RepairDto)) {
            return (T)(object)ModelToDto(repair);
        }
        if (typeof(T) == typeof(RepairDetailViewDto)) {
            return (T)(object)ModelToDetailViewDto(repair);
        }
        throw new InvalidOperationException($"Mapping for {typeof(T).Name} is not defined.");
    }

    private RepairDetailViewDto ModelToDetailViewDto(Repair repair) {
        return new RepairDetailViewDto {
            Id = repair.Id,
            Title = repair.Title,
            Description = repair.Description,
            Cost = repair.Cost,
            RepairDate = repair.RepairDate,
            CreatedAt = repair.CreatedAt,
            UpdatedAt = repair.UpdatedAt,
            UnitId = repair.UnitId,
            Documents = repair.Documents.Select(x => new RepairDocumentsDto {
                Id = x.Id,
                Description = x.Description,
                FilePath = x.FilePath,
                CreatedAt = x.CreatedAt,
                RepairId = x.RepairId,
            }).ToList() ?? new List<RepairDocumentsDto>(),
        };
    }

    internal RepairDto ModelToDto(Repair repair) {
        return new RepairDto {
            Id = repair.Id,
            Title = repair.Title,
            Description = repair.Description,
            Cost = repair.Cost,
            RepairDate = repair.RepairDate,
            UnitId = repair.UnitId,
        };
    }

    private Repair DtoToModel(RepairDto repairDto) {
        return new Repair {
            Cost = repairDto.Cost,
            Description = repairDto.Description,
            RepairDate = repairDto.RepairDate,
            Title = repairDto.Title,
            UnitId = repairDto.UnitId,
        };
    }

    internal IEnumerable<RepairDto> GetAllRepairsByUserId(string? v) {
        return _context.Properties.Where(x => x.UserId == v).SelectMany(x => x.Units).SelectMany(x => x.Repairs).Select(ModelToDtoGen<RepairDto>).ToList();
    }

    internal IEnumerable<int> GetAllRepairsIdsByUserId(string? v) {
        return _context.Properties.Where(x => x.UserId == v).SelectMany(x => x.Units).SelectMany(x => x.Repairs).Select(u => u.Id).ToList();
    }

    internal IEnumerable<SelectListItem> GetUnits(string? v) {
        return _context.Properties.Where(x => x.UserId == v).SelectMany(x => x.Units).Select(u => new SelectListItem {
            Value = u.Id.ToString(),
            Text = _localizer[u.UnitType.ToString()] + ": " + u.Name,
        }).ToList();
    }


    internal async Task CreateRepairAsync(RepairDto repairDto, IFormCollection files, string v) {

        var repair = DtoToModel(repairDto);
        _context.Repairs.Add(repair);
        await _context.SaveChangesAsync();
        List<RepairDocumentsDto> repairDocumentsDtos = await SaveFilesAsync(files, repair.Id, v);
        List<RepairDocument> repairDocuments = repairDocumentsDtos.Select(x => new RepairDocument {
            FilePath = x.FilePath,
            Description = x.Description,
            CreatedAt = x.CreatedAt,
            RepairId = x.RepairId,
        }).ToList();
        repair.Documents = repairDocuments;
        await _context.SaveChangesAsync();
    }

    private async Task<List<RepairDocumentsDto>> SaveFilesAsync(IFormCollection files, int repairId, string v) {
        List<RepairDocumentsDto> repairDocumentsDtos = new List<RepairDocumentsDto>();
        foreach (var file in files.Files) {
            if (file.Length > 0 && file.Length < 10 * 1024 * 1024 && IsImage(file)) {
                var uploadsDir = Path.Combine("wwwroot", "uploads", "repairs", v);
                if (!Directory.Exists(uploadsDir)) {
                    Directory.CreateDirectory(uploadsDir);
                }
                // Unikátní název souboru
                var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                try {
                    using (var fileStream = new FileStream(filePath, FileMode.Create)) {
                        await file.CopyToAsync(fileStream);
                    }
                    var repairDocument = new RepairDocumentsDto {
                        FilePath = filePath.Replace("wwwroot\\", "").Replace("\\", "/"),
                        Description = file.FileName,
                        CreatedAt = DateTime.Now,
                        RepairId = repairId,
                    };
                    repairDocumentsDtos.Add(repairDocument);
                }
                catch (Exception ex) {

                    Console.WriteLine($"Error while saving file: {ex.Message}");
                    continue;
                }
            }
        }
        return repairDocumentsDtos;
    }

    private bool IsImage(IFormFile file) {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf" };
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp", "application/pdf" };

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Validation extension and mime type
        return allowedExtensions.Contains(fileExtension) && allowedMimeTypes.Contains(file.ContentType);
    }


    internal async Task<T> FindByIdAsync<T>(int id) {
        return ModelToDtoGen<T>(await _context.Repairs.Include(r => r.Documents).FirstOrDefaultAsync(x => x.Id == id));
    }

    internal async Task EditRepairAsync(RepairDetailViewDto repairDto, int id, Dictionary<int, string> descriptions) {
        var repairToEdit = await _context.Repairs.FirstOrDefaultAsync(x => x.Id == id);
        if (repairToEdit != null) {
            repairToEdit.Title = repairDto.Title;
            repairToEdit.Description = repairDto.Description;
            repairToEdit.Cost = repairDto.Cost;
            repairToEdit.RepairDate = repairDto.RepairDate;
            repairToEdit.UnitId = repairDto.UnitId;
            repairToEdit.UpdatedAt = DateTime.Now;

            foreach (var entry in descriptions) {
                var document = await _context.RepairDocuments.FindAsync(entry.Key);
                if (document != null) {
                    document.Description = entry.Value;
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    internal async Task DeleteRepairAsync(int id) {
        var repairToDelete = _context.Repairs
            .Include(r => r.Documents)
            .FirstOrDefault(r => r.Id == id);
        if (repairToDelete != null) {
        //    _context.Repairs.Remove(repairToDelete);
            repairToDelete.IsDeleted = true;
            repairToDelete.DeletedAt = DateTime.Now;
            foreach (var document in repairToDelete.Documents) {
                document.IsDeleted = true;
                document.DeletedAt = DateTime.Now;
            }
        }
        await _context.SaveChangesAsync();
    }
    internal bool RepairExists(int id) {
        return _context.Repairs.Any(e => e.Id == id);
    }
}
