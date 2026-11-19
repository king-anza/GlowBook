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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("mobile", policy =>
{
    if (allowedOrigins.Length != 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

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
    app.MapPost("/api/v1/development/gqeberha-demo-data", async (GlowBookDbContext db, CancellationToken ct) => Results.Ok(new { seeded = await GlowBook.Api.DevelopmentDemoData.SeedAsync(db, ct) }));

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
professional.MapPost("/business", async (BusinessRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => Results.Ok(await service.SaveAsync(UserId(user), user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "Owner", request, ct))).AddEndpointFilter<ValidationFilter<BusinessRequest>>();
professional.MapPatch("/business", async (BusinessRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => Results.Ok(await service.SaveAsync(UserId(user), user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "Owner", request, ct))).AddEndpointFilter<ValidationFilter<BusinessRequest>>();
professional.MapPost("/business/submit", async (ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.SubmitAsync(UserId(user), ct) ? Results.NoContent() : Results.BadRequest());
professional.MapPost("/staff", async (StaffRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.AddStaffAsync(UserId(user), request, ct) is { } staff ? Results.Ok(staff) : Results.BadRequest()).AddEndpointFilter<ValidationFilter<StaffRequest>>();
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
professional.MapPost("/staff/{staffId:guid}/blocked-times", async (Guid staffId, BlockedTimeRequest request, ClaimsPrincipal user, IAvailabilityService service, CancellationToken ct) => await service.AddBlockedTimeAsync(UserId(user), staffId, request, ct) ? Results.NoContent() : Results.BadRequest()).AddEndpointFilter<ValidationFilter<BlockedTimeRequest>>();
app.MapGet("/api/v1/staff/{staffId:guid}/availability", (Guid staffId, Guid offeringId, DateOnly from, DateOnly to, IAvailabilityService service, CancellationToken ct) => service.SlotsAsync(staffId, offeringId, from, to, ct));
app.MapGet("/api/v1/businesses", (string? search, Guid? categoryId, string? province, string? city, string? suburb, decimal? minPrice, decimal? maxPrice, int page, int pageSize, IDiscoveryService service, CancellationToken ct) => service.SearchAsync(new DiscoveryQuery(search, categoryId, province, city, suburb, minPrice, maxPrice, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct));
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

var admin = app.MapGroup("/api/v1/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
admin.MapGet("/businesses/pending", (IProfessionalService service, CancellationToken ct) => service.PendingAsync(ct));
admin.MapGet("/businesses", (IProfessionalService service, CancellationToken ct) => service.ManagedAsync(ct));
admin.MapPost("/businesses/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Approved, null, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/reject", async (Guid id, BusinessReviewRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Rejected, request.Reason, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/suspend", async (Guid id, BusinessReviewRequest request, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Suspended, request.Reason, ct) ? Results.NoContent() : Results.NotFound());
admin.MapPost("/businesses/{id:guid}/reactivate", async (Guid id, ClaimsPrincipal user, IProfessionalService service, CancellationToken ct) => await service.ReviewAsync(UserId(user), id, BusinessApprovalStatus.Approved, null, ct) ? Results.NoContent() : Results.NotFound());
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
