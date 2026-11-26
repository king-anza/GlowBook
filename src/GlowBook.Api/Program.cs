using GlowBook.Application;
using GlowBook.Application.Identity;
using GlowBook.Infrastructure;
using GlowBook.Infrastructure.Identity;
using GlowBook.Application.Professionals;
using GlowBook.Domain.Professionals;
using GlowBook.Application.Catalogue;
using GlowBook.Application.Availability;
using GlowBook.Application.Discovery;
using GlowBook.Application.Bookings;
using GlowBook.Domain.Bookings;
using GlowBook.Application.Reviews;
using CustomerReviewRequest = GlowBook.Application.Reviews.ReviewRequest;
using BusinessReviewRequest = GlowBook.Application.Professionals.ReviewRequest;
using GlowBook.Application.Accounts;
using GlowBook.Application.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using GlowBook.Application.Spaces;
using GlowBook.Domain.Spaces;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is required.");
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var tokenStamp = context.Principal?.FindFirstValue("security_stamp");
            if (!Guid.TryParse(subject, out var userId) || string.IsNullOrWhiteSpace(tokenStamp))
            {
                context.Fail("The access token is no longer valid.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<GlowBookDbContext>();
            var user = await db.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.AccountStatus, x.EmailConfirmed, x.SecurityStamp })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
            if (user is null || user.AccountStatus != "Active" || !user.EmailConfirmed || user.SecurityStamp != tokenStamp)
                context.Fail("The access token is no longer valid.");
        }
    };
});
builder.Services.AddAuthorization();
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowedOrigins = configuredOrigins
    .Concat(["https://localhost", "capacitor://localhost", "ionic://localhost"])
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
builder.Services.AddCors(options => options.AddPolicy("mobile", policy =>
{
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
    await using var demoScope = app.Services.CreateAsyncScope();
    var demoDatabase = demoScope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
    await demoDatabase.Database.MigrateAsync();
    if (!await demoDatabase.Businesses.AnyAsync())
    {
        var seeded = await GlowBook.Api.DemoDataSeeder.SeedAsync(demoDatabase, CancellationToken.None);
        app.Logger.LogInformation("Seeded {DemoSalonCount} demo salons because the production marketplace was empty.", seeded);
    }
}

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var supplied) && Guid.TryParse(supplied, out _)
        ? supplied.ToString()
        : Guid.NewGuid().ToString();
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })) await next();
});
app.UseExceptionHandler(new Microsoft.AspNetCore.Builder.ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception switch
    {
        Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status500InternalServerError
    }
});
app.UseStatusCodePages(async (Microsoft.AspNetCore.Diagnostics.StatusCodeContext statusContext) =>
{
    var response = statusContext.HttpContext.Response;
    var title = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status401Unauthorized => "Authentication required",
        StatusCodes.Status403Forbidden => "Access denied",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Request conflict",
        StatusCodes.Status429TooManyRequests => "Too many requests",
        _ => "Request failed"
    };
    response.ContentType = "application/problem+json";
    await response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { type = $"https://httpstatuses.com/{response.StatusCode}", title, status = response.StatusCode, traceId = statusContext.HttpContext.TraceIdentifier }));
});
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("mobile");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy")))
    .WithName("GetHealth")
    .WithTags("Health");

if (app.Environment.IsDevelopment())
    app.MapPost("/api/v1/development/gqeberha-demo-data", async (GlowBookDbContext db, CancellationToken ct) => Results.Ok(new { seeded = await GlowBook.Api.DemoDataSeeder.SeedAsync(db, ct) }));

var auth = app.MapGroup("/api/v1/auth").RequireRateLimiting("auth");
auth.MapPost("/register/customer", (RegisterRequest request, IIdentityService identity, CancellationToken ct) => Register(request, "Customer", identity, ct)).AddEndpointFilter<ValidationFilter<RegisterRequest>>();
auth.MapPost("/register/professional", (RegisterRequest request, IIdentityService identity, CancellationToken ct) => Register(request, "Professional", identity, ct)).AddEndpointFilter<ValidationFilter<RegisterRequest>>();
auth.MapPost("/login", async (LoginRequest request, IIdentityService identity, CancellationToken ct) => ToHttpResult(await identity.LoginAsync(request, ct))).AddEndpointFilter<ValidationFilter<LoginRequest>>();
auth.MapPost("/refresh", async (RefreshRequest request, IIdentityService identity, CancellationToken ct) => ToHttpResult(await identity.RefreshAsync(request.RefreshToken, ct))).AddEndpointFilter<ValidationFilter<RefreshRequest>>();
auth.MapPost("/logout", async (RefreshRequest request, IIdentityService identity, CancellationToken ct) => { await identity.LogoutAsync(request.RefreshToken, ct); return Results.NoContent(); });
auth.MapPost("/verify-email", async (VerifyEmailRequest request, IIdentityService identity, CancellationToken ct) => await identity.VerifyEmailAsync(request, ct) ? Results.NoContent() : Results.BadRequest());
auth.MapPost("/resend-verification", async (EmailRequest request, IIdentityService identity, CancellationToken ct) => { await identity.RequestEmailVerificationAsync(request.Email, ct); return Results.Accepted(); });
auth.MapPost("/forgot-password", async (EmailRequest request, IIdentityService identity, CancellationToken ct) => { await identity.RequestPasswordResetAsync(request.Email, ct); return Results.Accepted(); });
auth.MapPost("/reset-password", async (ResetPasswordRequest request, IIdentityService identity, CancellationToken ct) => { await identity.ResetPasswordAsync(request, ct); return Results.NoContent(); });

app.MapGet("/api/v1/me", async (ClaimsPrincipal principal, IIdentityService identity, CancellationToken ct) =>
{
    var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(subject, out var userId) && await identity.GetCurrentUserAsync(userId, ct) is { } user ? Results.Ok(user) : Results.NotFound();
}).RequireAuthorization();
app.MapGet("/api/v1/me/data", async (ClaimsPrincipal user, IAccountService service, CancellationToken ct) => await service.ExportAsync(UserId(user), ct) is { } data ? Results.Ok(data) : Results.NotFound()).RequireAuthorization();
app.MapPatch("/api/v1/me", async (UpdateAccountRequest request, ClaimsPrincipal user, IAccountService service, CancellationToken ct) => await service.UpdateAsync(UserId(user), request, ct) is { } data ? Results.Ok(data) : Results.BadRequest()).RequireAuthorization().AddEndpointFilter<ValidationFilter<UpdateAccountRequest>>();
app.MapDelete("/api/v1/me", async (ClaimsPrincipal user, IAccountService service, CancellationToken ct) => await service.AnonymiseAsync(UserId(user), ct) ? Results.NoContent() : Results.BadRequest()).RequireAuthorization();

var professional = app.MapGroup("/api/v1/professional").RequireAuthorization(policy => policy.RequireRole("Professional"));
professional.MapGet("/business", async (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.GetAsync(UserId(user), ct) is { } business ? Results.Ok(business) : Results.NotFound());
professional.MapGet("/onboarding", (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => service.OnboardingAsync(UserId(user), ct));
professional.MapGet("/dashboard", (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => service.DashboardAsync(UserId(user), ct));
professional.MapGet("/insights", (int periodDays, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => service.InsightsAsync(UserId(user), periodDays, ct));
professional.MapPost("/business", async (BusinessRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => Results.Ok(await service.SaveAsync(UserId(user), user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "Owner", request, ct))).AddEndpointFilter<ValidationFilter<BusinessRequest>>();
professional.MapPatch("/business", async (BusinessRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => Results.Ok(await service.SaveAsync(UserId(user), user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "Owner", request, ct))).AddEndpointFilter<ValidationFilter<BusinessRequest>>();
professional.MapPost("/business/submit", async (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.SubmitAsync(UserId(user), ct) ? Results.NoContent() : Results.BadRequest());
professional.MapPost("/staff", async (StaffRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.AddStaffAsync(UserId(user), request, ct) is { } staff ? Results.Ok(staff) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<StaffRequest>>();
professional.MapPatch("/staff/{staffId:guid}", async (Guid staffId, StaffRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.UpdateStaffAsync(UserId(user), staffId, request, ct) is { } staff ? Results.Ok(staff) : Results.NotFound()).AddEndpointFilter<ValidationFilter<StaffRequest>>();
professional.MapPost("/portfolio-images", async (PortfolioImageRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.AddPortfolioImageAsync(UserId(user), request, ct) is { } image ? Results.Ok(image) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<PortfolioImageRequest>>();
professional.MapGet("/portfolio-images", (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => service.PortfolioImagesAsync(UserId(user), ct));
professional.MapDelete("/portfolio-images/{id:guid}", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.DeletePortfolioImageAsync(UserId(user), id, ct) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/v1/categories", (ICatalogueService service, CancellationToken ct) => service.CategoriesAsync(ct));
professional.MapGet("/services", (ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => service.ServicesAsync(UserId(user), ct));
professional.MapPost("/services", async (ServiceRequest request, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SaveServiceAsync(UserId(user), null, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<ServiceRequest>>();
professional.MapPatch("/services/{id:guid}", async (Guid id, ServiceRequest request, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SaveServiceAsync(UserId(user), id, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<ServiceRequest>>();
professional.MapPost("/services/{id:guid}/offerings", async (Guid id, OfferingRequest request, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SaveOfferingAsync(UserId(user), id, null, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<OfferingRequest>>();
professional.MapPatch("/services/{serviceId:guid}/offerings/{id:guid}", async (Guid serviceId, Guid id, OfferingRequest request, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SaveOfferingAsync(UserId(user), serviceId, id, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<OfferingRequest>>();
professional.MapPost("/services/{id:guid}/activate", async (Guid id, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SetServiceActiveAsync(UserId(user), id, true, ct) ? Results.NoContent() : Results.NotFound());
professional.MapPost("/services/{id:guid}/deactivate", async (Guid id, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SetServiceActiveAsync(UserId(user), id, false, ct) ? Results.NoContent() : Results.NotFound());
professional.MapPost("/offerings/{id:guid}/activate", async (Guid id, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SetOfferingActiveAsync(UserId(user), id, true, ct) ? Results.NoContent() : Results.NotFound());
professional.MapPost("/offerings/{id:guid}/deactivate", async (Guid id, ClaimsPrincipal user, ICatalogueService service, CancellationToken ct) => await service.SetOfferingActiveAsync(UserId(user), id, false, ct) ? Results.NoContent() : Results.NotFound());
professional.MapGet("/staff/{staffId:guid}/weekly-availability", (Guid staffId, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => service.GetWeeklyAsync(UserId(user), staffId, ct));
professional.MapPut("/staff/{staffId:guid}/weekly-availability", async (Guid staffId, WeeklyAvailabilityRequest[] request, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.ReplaceWeeklyAsync(UserId(user), staffId, request, ct) ? Results.NoContent() : Results.BadRequest());
professional.MapPost("/staff/{staffId:guid}/availability-exceptions", async (Guid staffId, ExceptionRequest request, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.AddExceptionAsync(UserId(user), staffId, request, ct) ? Results.NoContent() : Results.BadRequest()).AddEndpointFilter<ValidationFilter<ExceptionRequest>>();
professional.MapPost("/staff/{staffId:guid}/blocked-times", async (Guid staffId, BlockedTimeRequest request, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.AddBlockedTimeWithConflictCheckAsync(UserId(user), staffId, request, ct) switch
{
    BlockedTimeResult.Added => Results.NoContent(),
    BlockedTimeResult.NotOwned => Results.NotFound(),
    BlockedTimeResult.BookingConflict => Results.Conflict(new { detail = "Blocked time overlaps a pending or confirmed booking. Move the booking before blocking this time." }),
    _ => Results.BadRequest(new { detail = "Choose a valid start and end time no more than 31 days apart." })
}).AddEndpointFilter<ValidationFilter<BlockedTimeRequest>>();
professional.MapDelete("/blocked-times/{id:guid}", async (Guid id, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.DeleteBlockedTimeAsync(UserId(user), id, ct) ? Results.NoContent() : Results.NotFound());
professional.MapGet("/calendar", async (DateOnly from, DateOnly to, Guid? staffId, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.CalendarAsync(UserId(user), from, to, staffId, ct) is { } calendar ? Results.Ok(calendar) : Results.BadRequest(new { detail = "Choose an owned staff member and a date range of up to seven days." }));
app.MapGet("/api/v1/staff/{staffId:guid}/availability", (Guid staffId, Guid offeringId, DateOnly from, DateOnly to, IAvailabilityService service, CancellationToken ct) => service.SlotsAsync(staffId, offeringId, from, to, ct));
app.MapGet("/api/v1/businesses", (string? search, Guid? categoryId, string? province, string? city, string? suburb, decimal? minPrice, decimal? maxPrice, double? latitude, double? longitude, double? radiusKm, string? sort, int page, int pageSize, IDiscoveryService service, CancellationToken ct) => service.SearchAsync(new DiscoveryQuery(search, categoryId, province, city, suburb, minPrice, maxPrice, latitude, longitude, radiusKm, sort, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct));
app.MapGet("/api/v1/businesses/{id:guid}", async (Guid id, IDiscoveryService service, CancellationToken ct) => await service.DetailAsync(id, ct) is { } business ? Results.Ok(business) : Results.NotFound());

var customer = app.MapGroup("/api/v1/bookings").RequireAuthorization(policy => policy.RequireRole("Customer"));
customer.MapPost("/", async (CreateBookingRequest request, ClaimsPrincipal user, IBookingService service, CancellationToken ct) => await service.CreateAsync(UserId(user), request, ct) is { } booking ? Results.Ok(booking) : Results.Conflict()).AddEndpointFilter<ValidationFilter<CreateBookingRequest>>();
customer.MapGet("/", (ClaimsPrincipal user, IBookingService service, CancellationToken ct) => service.CustomerBookingsAsync(UserId(user), ct));
customer.MapPost("/{id:guid}/cancel", async (Guid id, BookingActionRequest request, ClaimsPrincipal user, IBookingService service, CancellationToken ct) => BookingTransitionResponse(await service.TransitionAsync(UserId(user), false, id, BookingStatus.Cancelled, request, ct)));
customer.MapPost("/{id:guid}/review", async (Guid id, CustomerReviewRequest request, ClaimsPrincipal user, IReviewService service, CancellationToken ct) => await service.CreateAsync(UserId(user), id, request, ct) is { } review ? Results.Ok(review) : Results.Conflict()).AddEndpointFilter<ValidationFilter<CustomerReviewRequest>>();
app.MapPatch("/api/v1/reviews/{id:guid}", async (Guid id, CustomerReviewRequest request, ClaimsPrincipal user, IReviewService service, CancellationToken ct) => await service.UpdateAsync(UserId(user), id, request, ct) is { } review ? Results.Ok(review) : Results.Conflict()).RequireAuthorization(policy => policy.RequireRole("Customer")).AddEndpointFilter<ValidationFilter<CustomerReviewRequest>>();
app.MapDelete("/api/v1/reviews/{id:guid}", async (Guid id, ClaimsPrincipal user, IReviewService service, CancellationToken ct) => await service.DeleteAsync(UserId(user), id, ct) ? Results.NoContent() : Results.NotFound()).RequireAuthorization(policy => policy.RequireRole("Customer"));
app.MapGet("/api/v1/reviews", (ClaimsPrincipal user, IReviewService service, CancellationToken ct) => service.ForCustomerAsync(UserId(user), ct)).RequireAuthorization(policy => policy.RequireRole("Customer"));
app.MapGet("/api/v1/businesses/{id:guid}/reviews", (Guid id, int page, int pageSize, IReviewService service, CancellationToken ct) => service.ForBusinessAsync(id, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct));
professional.MapGet("/bookings", (ClaimsPrincipal user, IBookingService service, CancellationToken ct) => service.ProfessionalBookingsAsync(UserId(user), ct));
professional.MapPost("/bookings/{id:guid}/{action}", async (Guid id, string action, BookingActionRequest request, ClaimsPrincipal user, IBookingService service, CancellationToken ct) => ParseBookingAction(action) is { } target ? BookingTransitionResponse(await service.TransitionAsync(UserId(user), true, id, target, request, ct)) : BookingTransitionResponse(BookingTransitionResult.InvalidTransition));

app.MapGet("/api/v1/spaces", (string? city, OpportunityType? type, WorkspaceType? workspaceType, ISpaceService service, CancellationToken ct) => service.DiscoverAsync(city, type, workspaceType, ct));
professional.MapGet("/spaces", (ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => service.OwnerOpportunitiesAsync(UserId(user), ct));
professional.MapPost("/spaces", async (OpportunityRequest request, ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => await service.SaveAsync(UserId(user), null, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<OpportunityRequest>>();
professional.MapPatch("/spaces/{id:guid}", async (Guid id, OpportunityRequest request, ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => await service.SaveAsync(UserId(user), id, request, ct) is { } item ? Results.Ok(item) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<OpportunityRequest>>();
professional.MapPost("/spaces/{id:guid}/status/{status}", async (Guid id, OpportunityStatus status, ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => await service.SetStatusAsync(UserId(user), id, status, ct) ? Results.NoContent() : Results.BadRequest());
professional.MapPost("/spaces/{id:guid}/applications", async (Guid id, ApplicationRequest request, ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => await service.ApplyAsync(UserId(user), id, request, ct) is { } item ? Results.Ok(item) : Results.Conflict()).AddEndpointFilter<ValidationFilter<ApplicationRequest>>();
professional.MapGet("/space-applications/received", (ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => service.OwnerApplicationsAsync(UserId(user), ct));
professional.MapGet("/space-applications/mine", (ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => service.ApplicantApplicationsAsync(UserId(user), ct));
professional.MapPost("/space-applications/{id:guid}/{status}", async (Guid id, ApplicationStatus status, ClaimsPrincipal user, ISpaceService service, CancellationToken ct) => await service.ReviewApplicationAsync(UserId(user), id, status, ct) ? Results.NoContent() : Results.BadRequest());

var admin = app.MapGroup("/api/v1/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
admin.MapGet("/businesses/pending", (IProfessionalService service, CancellationToken ct) => service.PendingAsync(ct));
admin.MapGet("/businesses", (IProfessionalService service, CancellationToken ct) => service.ManagedAsync(ct));
admin.MapPost("/businesses/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Approved, null, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/reject", async (Guid id, BusinessReviewRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Rejected, request.Reason, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/suspend", async (Guid id, BusinessReviewRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Suspended, request.Reason, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/reactivate", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Approved, null, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/demo", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.SetDemoAsync(UserId(user), id, true, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/real", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.SetDemoAsync(UserId(user), id, false, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/demo-data/seed", async (ClaimsPrincipal user, GlowBookDbContext db, CancellationToken ct) =>
{
    var seeded = await GlowBook.Api.DemoDataSeeder.SeedAsync(db, ct);
    db.AuditLogs.Add(new GlowBook.Domain.Operations.AuditLog { Id = Guid.NewGuid(), ActorUserId = UserId(user), Action = "DemoDataSeeded", SubjectType = "DemoData", SubjectId = Guid.Empty, Details = $"{seeded} demo salons created or refreshed", OccurredAtUtc = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { seeded });
});
admin.MapPost("/reviews/{id:guid}/hide", async (Guid id, ClaimsPrincipal user, IReviewService service, CancellationToken ct) => await service.SetHiddenAsync(UserId(user), id, true, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/reviews/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, IReviewService service, CancellationToken ct) => await service.SetHiddenAsync(UserId(user), id, false, ct) ? Results.NoContent() : Results.NotFound());
admin.MapGet("/reviews", (IReviewService service, CancellationToken ct) => service.ForModerationAsync(ct));
admin.MapGet("/users", (int page, int pageSize, IOperationsService service, CancellationToken ct) => service.UsersAsync(page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct));
admin.MapGet("/audit-logs", (int page, int pageSize, IOperationsService service, CancellationToken ct) => service.AuditAsync(page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct));
admin.MapGet("/bookings", (IBookingService service, CancellationToken ct) => service.AdminBookingsAsync(ct));
admin.MapPost("/portfolio-images/{id:guid}/hide", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ModeratePortfolioImageAsync(UserId(user), id, true, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/portfolio-images/{id:guid}/restore", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ModeratePortfolioImageAsync(UserId(user), id, false, ct) ? Results.NoContent() : Results.NotFound());
admin.MapGet("/portfolio-images", (IProfessionalService service, CancellationToken ct) => service.PortfolioModerationAsync(ct));

static async Task<IResult> Register(RegisterRequest request, string role, IIdentityService identity, CancellationToken cancellationToken)
{
    var result = await identity.RegisterAsync(request, role, cancellationToken);
    return result.Succeeded ? Results.Accepted() : ToHttpResult(result);
}

static IResult ToHttpResult(AuthResult result) => result.Succeeded
    ? Results.Ok(result.Tokens)
    : Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Authentication request failed", extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode, ["errors"] = result.Errors });

static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("Authenticated user has no subject claim."));
static BookingStatus? ParseBookingAction(string action) => action.ToLowerInvariant() switch { "confirm" => BookingStatus.Confirmed, "reject" => BookingStatus.Rejected, "cancel" => BookingStatus.Cancelled, "complete" => BookingStatus.Completed, "no-show" => BookingStatus.NoShow, _ => null };
static IResult BookingTransitionResponse(BookingTransitionResult result) => result == BookingTransitionResult.Success
    ? Results.NoContent()
    : Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Booking transition failed", extensions: new Dictionary<string, object?> { ["code"] = result switch { BookingTransitionResult.Stale => "booking_version_conflict", BookingTransitionResult.InvalidTransition => "booking_transition_invalid", _ => "booking_transition_rejected" } });

app.Run();

internal sealed record HealthResponse(string Status);

public partial class Program;
