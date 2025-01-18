using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebPWrecover.Services;


namespace MujDomecek;
public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args); 
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        //var connectionString = builder.Configuration.GetConnectionString("MonsterASPConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        //var connectionString = builder.Configuration.GetConnectionString("MonsterASPConnectionDeployed") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<AppUser>(options => {
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireDigit = true; // Numbers are required
            options.Password.RequiredLength = 6; // Minimum length of the password
            options.Password.RequireNonAlphanumeric = false; // Special characters are not required
            options.Password.RequireUppercase = true; // Uppercase letters are required
            options.Password.RequireLowercase = true;  // Lowercase letters are required
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddScoped<PropertyService>();
        builder.Services.AddScoped<UnitService>();
        builder.Services.AddScoped<RepairService>();
        builder.Services.AddScoped<AdminService>();

        // Add localization services
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        // Supported cultures
        var supportedCultures = new[] {
            new CultureInfo("en"),
            new CultureInfo("cs")
        };
        // Localization configuration
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("cs");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

        // Email sender
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);

        var app = builder.Build();

        // Localization
        var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseMigrationsEndPoint();
            // Used for hiding sensitive informations
            builder.Configuration.AddUserSecrets<Program>();

        }
        else {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            //For testing when app is deployed
            //app.UseDeveloperExceptionPage();
        }

        // Middleware for handling exceptions
        app.Use(async (context, next) =>
        {
            try {
                await next();
            }
            catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        });


        app.UseHttpsRedirection();
        app.UseStaticFiles();


        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}
