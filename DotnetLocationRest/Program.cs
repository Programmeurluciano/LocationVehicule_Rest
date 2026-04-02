using DotnetLocationRest.Data;
using DotnetLocationRest.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Enregistrement des services personnalisés
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<VehiculeRepository>();
builder.Services.AddScoped<ReservationRepository>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<JwtService>();

// Configuration de l'authentification JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configuration CORS pour autoriser Angular/React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Messages de démarrage avec style
Console.WriteLine("");
Console.WriteLine("╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║                                                        ║");
Console.WriteLine("║      🚗 API LOCATION DE VÉHICULES - DÉMARRÉE ! 🚗     ║");
Console.WriteLine("║                                                        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine("");
Console.WriteLine("📍 URL de base        : http://localhost:5000");
Console.WriteLine("🔐 Authentification   : JWT activée");
Console.WriteLine("🗄️  Base de données   : LocationVehiculeDB");
Console.WriteLine("");
Console.WriteLine("📚 ENDPOINTS DISPONIBLES :");
Console.WriteLine("───────────────────────────────────────────────────────");
Console.WriteLine("  🔓 PUBLICS (sans authentification)");
Console.WriteLine("    POST   /api/auth/register");
Console.WriteLine("    POST   /api/auth/login");
Console.WriteLine("    GET    /api/vehicules");
Console.WriteLine("    GET    /api/vehicules/{id}");
Console.WriteLine("    GET    /api/vehicules/categories");
Console.WriteLine("    POST   /api/reservations/calculer-prix");
Console.WriteLine("    POST   /api/reservations/verifier-disponibilite");
Console.WriteLine("");
Console.WriteLine("  🔒 PROTÉGÉS (authentification requise)");
Console.WriteLine("    GET    /api/auth/profile");
Console.WriteLine("    POST   /api/reservations");
Console.WriteLine("    GET    /api/reservations/mes-reservations");
Console.WriteLine("    GET    /api/reservations/{id}");
Console.WriteLine("    PUT    /api/reservations/{id}/annuler");
Console.WriteLine("───────────────────────────────────────────────────────");
Console.WriteLine("");
Console.WriteLine("✅ Serveur prêt à recevoir des requêtes !");
Console.WriteLine("");

app.Run();