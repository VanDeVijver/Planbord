using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Planbord.Core;
using Planbord.Web;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("nl-BE");

var builder = WebApplication.CreateBuilder(args);
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<PlanbordDb>(o => o.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ontbreekt (user secrets of omgevingsvariabele).")));
builder.Services.AddDataProtection().PersistKeysToDbContext<PlanbordDb>();
builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<PlanbordDb>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/Geweigerd";
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Planner", p => p.RequireRole(Rollen.Eigenaar, Rollen.Planner));
    o.AddPolicy("Eigenaar", p => p.RequireRole(Rollen.Eigenaar));
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddRazorPages(o =>
{
    foreach (var map in new[] { "/Planning", "/Projecten", "/Medewerkers", "/Berichten", "/Dagrapporten", "/Overzichten" })
        o.Conventions.AuthorizeFolder(map, "Planner");
    o.Conventions.AuthorizeFolder("/Gebruikers", "Eigenaar");
    o.Conventions.AllowAnonymousToFolder("/Account");
    o.Conventions.AllowAnonymousToPage("/Privacy");
    o.Conventions.AllowAnonymousToPage("/Error");
    o.Conventions.AllowAnonymousToPage("/Status");
});
builder.Services.AddScoped<PlanbordService>();
builder.Services.AddSingleton<Signalen>();
builder.Services.AddHealthChecks();
if (!builder.Environment.IsDevelopment())
    builder.Services.AddHttpsRedirection(o => o.HttpsPort = 443);
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // Render terminates TLS at its proxy.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlanbordDb>();
    await db.Database.MigrateAsync();
    var rollen = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var rol in Rollen.Alle)
        if (!await rollen.RoleExistsAsync(rol)) await rollen.CreateAsync(new IdentityRole(rol));

    var email = app.Configuration["Admin:Email"];
    var wachtwoord = app.Configuration["Admin:Password"];
    var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(wachtwoord) && await users.FindByEmailAsync(email) is null)
    {
        var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var res = await users.CreateAsync(admin, wachtwoord);
        if (!res.Succeeded) throw new InvalidOperationException("Admin aanmaken mislukt: " + string.Join("; ", res.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(admin, Rollen.Eigenaar);
    }

    // A fresh local database gets the sample data, so the app is demo-ready without a manual reset.
    if (app.Environment.IsDevelopment() && !await db.Medewerkers.AnyAsync())
        await DemoData.Reset(db, Huidig.Vandaag);
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Status", "?code={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapGet("/signalen", (HttpContext ctx, Signalen s) => s.Stream(ctx));
app.MapRazorPages();

app.Run();
