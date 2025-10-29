using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureApi.Data;
using SecureApi.Security;                  // <- middlewares de seguridad
using Microsoft.AspNetCore.RateLimiting;   // <- rate limiting (opcional)
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ======================= SERVICES =======================

// DbContext (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Hash de contraseñas (BCrypt)
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Controllers + Swagger con esquema de seguridad Bearer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Secure API", Version = "v1" });

    // Seguridad Bearer en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce tu token con el formato: **Bearer {token}**"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// JWT: validación estricta
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
        opt.SaveToken = false;
        // opt.RequireHttpsMetadata = true; // Actívalo en producción con HTTPS real
    });

builder.Services.AddAuthorization();

// CORS afinado (solo FE local)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ng", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Rate limiting (opcional) — usamos en /auth/login
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("loginLimiter", options =>
    {
        options.Window = TimeSpan.FromMinutes(1);
        options.PermitLimit = 10; // 10 intentos/min por IP
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    }));

// ======================= APP PIPELINE =======================

var app = builder.Build();

// Manejo global de errores (debe ir lo más arriba posible)
app.UseGlobalErrorHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // HSTS solo en no-desarrollo
    app.UseHsts();
}

app.UseHttpsRedirection();

// Encabezados de seguridad
app.UseSecurityHeaders();

// CORS
app.UseCors("ng");

// AuthN/AuthZ
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting (si lo usas en /auth/login)
app.UseRateLimiter();

app.MapControllers();

app.Run();
