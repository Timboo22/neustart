using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Neustart.Api.Data;
using Neustart.Api.DTOs;
using Neustart.Api.Models;
using Neustart.Api.Services;
using Neustart.Api.Services.Contracts;

var builder = WebApplication.CreateBuilder(args);

// DB & Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Auth
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing!");

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Migrate() creates the database if it doesn't exist and applies all migrations.
    // This is the standard "clean" way for real databases like PostgreSQL.
    db.Database.Migrate();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");

api.MapPost("/register", async (RegisterRequest request, IRegisterService service) =>
{
    var response = await service.RegisterAsync(request);
    return response != null ? Results.Ok(response) : Results.BadRequest("Registration failed");
});

api.MapPost("/login", async (LoginRequest request, IRegisterService service) =>
{
    var response = await service.LoginAsync(request);
    return response != null ? Results.Ok(response) : Results.Unauthorized();
});

api.MapGet("/hello", [Microsoft.AspNetCore.Authorization.Authorize] () => "Hi");

app.Run();
