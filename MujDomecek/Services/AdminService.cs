
using Microsoft.EntityFrameworkCore;

namespace MujDomecek.Services {
    public class AdminService(ApplicationDbContext _context) {
        internal async Task CleanUpDatabaseAsync() {
            // Remove files
            var documentsToDelete = await _context.RepairDocuments
                .IgnoreQueryFilters()
                .Where(d => d.IsDeleted)
                .ToListAsync();
            
            foreach (var document in documentsToDelete) {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath);

                if (System.IO.File.Exists(fullPath)) {
                    Console.WriteLine(fullPath);
                    try {
                        System.IO.File.Delete(fullPath);
                        Console.WriteLine("Deleted");
                    }
                    catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            // Remove repair documents
            _context.RepairDocuments.RemoveRange(documentsToDelete);

            // Remove repairs
            var repairsToDelete = await _context.Repairs
                .IgnoreQueryFilters()
                .Where(r => r.IsDeleted)
                .ToListAsync();
            _context.Repairs.RemoveRange(repairsToDelete);

            // Remove units
            var unitsToDelete = await _context.Units
                .IgnoreQueryFilters()
                .Where(u => u.IsDeleted)
                .ToListAsync();
            _context.Units.RemoveRange(unitsToDelete);

            // Remove properties
            var propertiesToDelete = await _context.Properties
                .IgnoreQueryFilters()
                .Where(p => p.IsDeleted)
                .ToListAsync();
            _context.Properties.RemoveRange(propertiesToDelete);

            await _context.SaveChangesAsync();
        }

        internal async Task GetUsers() {

        }
    }
}
