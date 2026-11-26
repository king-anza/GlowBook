using Microsoft.Extensions.DependencyInjection;
using GlowBook.Application.Identity;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GlowBook.Application.Professionals;
using GlowBook.Infrastructure.Professionals;
using GlowBook.Application.Catalogue;
using GlowBook.Infrastructure.Catalogue;
using GlowBook.Application.Availability;
using GlowBook.Infrastructure.Availability;
using GlowBook.Application.Discovery;
using GlowBook.Infrastructure.Discovery;
using GlowBook.Application.Bookings;
using GlowBook.Infrastructure.Bookings;
using GlowBook.Application.Reviews;
using GlowBook.Infrastructure.Reviews;
using GlowBook.Application.Accounts;
using GlowBook.Infrastructure.Accounts;
using GlowBook.Application.Operations;
using GlowBook.Infrastructure.Operations;
using GlowBook.Application.Spaces;
using GlowBook.Infrastructure.Spaces;

namespace GlowBook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GlowBook")
            ?? throw new InvalidOperationException("ConnectionStrings:GlowBook is required.");
        services.AddDbContext<GlowBookDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<PublicAppOptions>(configuration.GetSection(PublicAppOptions.SectionName));
        services.Configure<AdminBootstrapOptions>(configuration.GetSection(AdminBootstrapOptions.SectionName));
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<GlowBookDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHostedService<AdminBootstrapService>();
        var resendApiKey = configuration[$"{ResendOptions.SectionName}:ApiKey"];
        var smtpHost = configuration[$"{SmtpOptions.SectionName}:Host"];
        if (string.IsNullOrWhiteSpace(resendApiKey) && string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddSingleton<IIdentityEmailSender, NullIdentityEmailSender>();
        }
        else
        {
            if (!Uri.TryCreate(configuration[$"{PublicAppOptions.SectionName}:BaseUrl"], UriKind.Absolute, out var publicAppUri) || publicAppUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("PublicApp:BaseUrl must be an absolute HTTPS URL when email delivery is enabled.");

            if (!string.IsNullOrWhiteSpace(resendApiKey))
            {
                if (string.IsNullOrWhiteSpace(configuration[$"{ResendOptions.SectionName}:FromAddress"]))
                    throw new InvalidOperationException("Resend:FromAddress is required when Resend is enabled.");
                services.AddHttpClient<IEmailClient, ResendEmailClient>();
            }
            else
            {
                services.AddSingleton<IEmailClient, SmtpEmailClient>();
            }

            services.AddSingleton<IIdentityEmailSender, SmtpIdentityEmailSender>();
            services.AddHostedService<NotificationDispatcher>();
        }
        services.AddScoped<IProfessionalService, ProfessionalService>();
        services.AddScoped<ICatalogueService, CatalogueService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IOperationsService, OperationsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHttpClient<IPushNotificationSender, FirebasePushNotificationSender>();
        services.AddHostedService<PushNotificationDispatcher>();
        services.AddHostedService<AppointmentReminderService>();
        services.AddScoped<ISpaceService, SpaceService>();
        return services;
    }
}
