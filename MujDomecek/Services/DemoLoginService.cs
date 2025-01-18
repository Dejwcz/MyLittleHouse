
namespace MujDomecek.Services {
    public class DemoLoginService(ApplicationDbContext _context) {
        internal async Task UpdateLastLogin(string userName) {
            _context.Users.First(u => u.Email == userName).LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
