using EventBookingSystem.Data;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database and seed roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Ensure Events table exists (for existing databases that were created before Event model was added)
        await EnsureEventsTableExistsAsync(context);
        
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Helper method to ensure Events table exists
static async Task EnsureEventsTableExistsAsync(ApplicationDbContext context)
{
    try
    {
        // Check if Events table exists by trying to query it
        await context.Database.ExecuteSqlRawAsync("SELECT TOP 1 Id FROM Events");
    }
    catch
    {
        // Table doesn't exist, create it
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE [Events] (
                [Id] int NOT NULL IDENTITY(1,1),
                [Name] nvarchar(200) NOT NULL,
                [Description] nvarchar(2000) NOT NULL,
                [EventDate] datetime2 NOT NULL,
                [EventTime] time NOT NULL,
                [Location] nvarchar(200) NOT NULL,
                [Capacity] int NOT NULL,
                [Price] decimal(18,2) NOT NULL,
                [ImagePath] nvarchar(500) NULL,
                [IsActive] bit NOT NULL,
                [CreatedDate] datetime2 NOT NULL,
                [UpdatedDate] datetime2 NULL,
                [CreatedBy] nvarchar(450) NULL,
                CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
            );
        ");
    }
}

app.Run();
