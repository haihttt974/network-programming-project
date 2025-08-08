using Microsoft.EntityFrameworkCore;
using DKyThucTap.Data;
using DKyThucTap.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using DKyThucTap.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DKyThucTapContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(30); // 30 seconds timeout
        // Disable retry strategy to allow manual transactions
        // sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CandidateOnly", policy =>
        policy.RequireClaim("RoleId", "1"));

    options.AddPolicy("RecruiterOnly", policy =>
        policy.RequireClaim("RoleId", "2"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("RoleId", "3"));

    options.AddPolicy("CandidateOrAdmin", policy =>
        policy.RequireClaim("RoleId", new[] { "1", "3" }));

    options.AddPolicy("RecruiterOrAdmin", policy =>
        policy.RequireClaim("RoleId", new[] { "2", "3" }));
});

// Add SignalR
builder.Services.AddSignalR();

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IOnlineUserService, OnlineUserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationIntegrationService, NotificationIntegrationService>();

// Add background services
builder.Services.AddHostedService<OnlineUserCleanupService>();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminHome}/{action=AdminDashboard}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map SignalR hubs
app.MapHub<DKyThucTap.Hubs.NotificationHub>("/notificationHub");

// Initialize default roles
using (var scope = app.Services.CreateScope())
{
    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        logger.LogInformation("Starting role initialization...");
        var success = await authorizationService.InitializeDefaultRolesAsync();

        if (success)
        {
            logger.LogInformation("Role initialization completed successfully");
        }
        else
        {
            logger.LogError("Role initialization failed");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Critical error during role initialization");
    }
}
app.MapHub<ChatHub>("/chathub");

app.Run();
