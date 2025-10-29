using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureApi.Data;
using SecureApi.DTOs;
using SecureApi.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;

namespace SecureApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _pwd;
    private readonly IConfiguration _cfg;

    public AuthController(AppDbContext db, IPasswordService pwd, IConfiguration cfg)
    {
        _db = db; _pwd = pwd; _cfg = cfg;
    }

    [HttpPost("login")]
    [EnableRateLimiting("loginLimiter")]       // <- agrega este atributo
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == req.Correo && u.Activo);
        if (user == null) return Unauthorized();

        // Si la contraseña guardada no parece hash BCrypt, la hashamos en el primer login.
        var stored = user.Contrasena ?? "";
        var looksHashed = stored.StartsWith("$2a$") || stored.StartsWith("$2b$") || stored.StartsWith("$2y$");

        if (!looksHashed)
        {
            // valida plano por única vez y migra a hash
            if (stored != req.Contrasena) return Unauthorized();
            user.Contrasena = _pwd.Hash(req.Contrasena);
            await _db.SaveChangesAsync();
        }
        else
        {
            if (!_pwd.Verify(req.Contrasena, stored)) return Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID_Usuario.ToString()),
            new(ClaimTypes.Name, user.Correo),
            new(ClaimTypes.Email, user.Correo),
            new(ClaimTypes.Role, user.Rol)
        };

        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(tokenStr));
    }
}
