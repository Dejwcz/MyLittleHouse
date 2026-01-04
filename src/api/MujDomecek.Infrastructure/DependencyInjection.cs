using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Infrastructure.Options;
using MujDomecek.Infrastructure.Identity;
using MujDomecek.Infrastructure.Persistence;
using MujDomecek.Infrastructure.Services;

namespace MujDomecek.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<ISyncQueueProcessor, LoggingSyncQueueProcessor>();
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        RegisterStorage(services, configuration);

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<CzechIdentityErrorDescriber>();

        return services;
    }

    private static void RegisterStorage(IServiceCollection services, IConfiguration configuration)
    {
        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();

        if (string.Equals(storageOptions.Provider, "local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IStorageService, LocalStorageService>();
            return;
        }

        services.AddSingleton<IAmazonS3>(_ => CreateS3Client(storageOptions));
        services.AddSingleton<IStorageService, S3StorageService>();
    }

    private static IAmazonS3 CreateS3Client(StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.S3.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.S3.ServiceUrl))
            config.ServiceURL = options.S3.ServiceUrl;

        if (!string.IsNullOrWhiteSpace(options.S3.Region))
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.S3.Region);

        if (!string.IsNullOrWhiteSpace(options.S3.AccessKey)
            && !string.IsNullOrWhiteSpace(options.S3.SecretKey))
        {
            var credentials = new BasicAWSCredentials(options.S3.AccessKey, options.S3.SecretKey);
            return new AmazonS3Client(credentials, config);
        }

        return new AmazonS3Client(config);
    }
}
