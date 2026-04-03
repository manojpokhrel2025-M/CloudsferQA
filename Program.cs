using CloudsferQA.Data;
using CloudsferQA.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Listen on all network interfaces so LAN users can access the app
builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Session}/{action=Start}/{id?}");

// ── Startup: ensure DB exists and seed initial data ──────────────────────
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env    = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    db.Database.EnsureCreated();

    // Add new tables that EnsureCreated won't add to existing databases
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ModuleOrders"" (
            ""ModuleName"" TEXT NOT NULL CONSTRAINT ""PK_ModuleOrders"" PRIMARY KEY,
            ""SortOrder""  INTEGER NOT NULL
        );");

    // Add Status column to Sessions if it doesn't exist yet (SQLite throws if already exists)
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Sessions"" ADD COLUMN ""Status"" TEXT NOT NULL DEFAULT 'In Progress';"); }
    catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Sessions"" ADD COLUMN ""IsArchived"" INTEGER NOT NULL DEFAULT 0;"); }
    catch { }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Sessions"" ADD COLUMN ""ArchivedAt"" TEXT NULL;"); }
    catch { }

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ActivityLogs"" (
            ""Id""          INTEGER NOT NULL CONSTRAINT ""PK_ActivityLogs"" PRIMARY KEY AUTOINCREMENT,
            ""Action""      TEXT NOT NULL,
            ""Details""     TEXT NOT NULL,
            ""PerformedBy"" TEXT NOT NULL,
            ""PerformedAt"" TEXT NOT NULL,
            ""Category""    TEXT NOT NULL
        );");

    DataSeeder.Seed(db, env.WebRootPath, config);
}

app.Run();
