using System.Globalization;
using KloroFil.Web.Data;
using KloroFil.Web.Models;
using KloroFil.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Culture invariante : les champs HTML envoient « 15.5 » et « 2026-11-09 », quelle que soit la langue du serveur.
// Les affichages en français (dates, nombres) sont formatés explicitement dans Affichage.cs.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Serveur et base sont dans appsettings.json ; le login SQL reste hors de GitHub (User Secrets ou variables
// d'environnement Sql__Utilisateur / Sql__MotDePasse). Sans login SQL, on se connecte avec le compte Windows.
var connexion = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("KloroFil"));
var utilisateurSql = builder.Configuration["Sql:Utilisateur"];
if (!string.IsNullOrEmpty(utilisateurSql))
{
    connexion.UserID = utilisateurSql;
    connexion.Password = builder.Configuration["Sql:MotDePasse"];
}
else if (string.IsNullOrEmpty(connexion.UserID))
{
    connexion.IntegratedSecurity = true;
}
builder.Services.AddDbContext<KloroFilDbContext>(o => o.UseSqlServer(connexion.ConnectionString));

builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddIdentity<Utilisateur, IdentityRole>(o =>
    {
        // Longueur plutôt que règles de complexité ; 5 échecs bloquent le compte 15 minutes.
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<KloroFilDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<ErreursIdentiteFr>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Compte/Connexion";
    o.AccessDeniedPath = "/Compte/AccesRefuse";
    o.Cookie.Name = "KloroFil.Session";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);   // ne s'applique qu'avec « Se souvenir de moi »
    o.SlidingExpiration = true;
});

// Un compte désactivé ou changé de rôle perd sa session en moins d'une minute.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

// Tout exige une connexion, sauf ce qui est marqué [AllowAnonymous].
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Veille réglementaire : lecture périodique des flux RSS / Atom déclarés dans « Sources de veille ».
builder.Services.AddHttpClient(LecteurFlux.NomClient, c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("KloroFil/1.0 (veille reglementaire)");
});
builder.Services.AddScoped<LecteurFlux>();
builder.Services.AddScoped<VeilleService>();
builder.Services.AddHostedService<VeillePlanifiee>();

var app = builder.Build();

// Création / mise à jour de la base et des rôles au démarrage.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KloroFilDbContext>();
    await db.Database.MigrateAsync();

    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in RolesKloroFil.Tous)
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();
