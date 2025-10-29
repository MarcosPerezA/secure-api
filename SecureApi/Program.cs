using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SecureApi.Data;
using SecureApi.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// 1) SERVICES
// ======================================

// DbContext (SQL Azure)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// (Opcional) Servicio de contraseñas si lo usas
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + esquema de seguridad Bearer
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Secure API", Version = "v1" });
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

// JWT inline (lee Jwt:* desde Variables de entorno / appsettings)
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado");
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];
var keyBytes = Encoding.UTF8.GetBytes(key);

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
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

// CORS (agrega aquí tu SWA cuando lo tengas)
builder.Services.AddCors(options =>
{
    options.AddPolicy("ng", policy =>
        policy.WithOrigins("http://localhost:4200") // + "https://<tu-swa>.azurestaticapps.net"
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Rate limiting opcional para /auth/login
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("loginLimiter", options =>
    {
        options.Window = TimeSpan.FromMinutes(1);
        options.PermitLimit = 10;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    }));

// ======================================
// 2) APP PIPELINE
// ======================================
var app = builder.Build();

// Manejo global de errores primero
app.UseGlobalErrorHandler();

// Swagger SIEMPRE (útil para demo en Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Secure API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Headers de seguridad
app.UseSecurityHeaders();

// CORS
app.UseCors("ng");

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Rate limiter (si lo usas en el AuthController)
app.UseRateLimiter();

app.MapControllers();

app.Run();
