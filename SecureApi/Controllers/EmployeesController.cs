using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using System.Security.Claims;

namespace SecureApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EmployeesController(AppDbContext db) { _db = db; }

    // Admin: ve todos (nombre + correo)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Usuarios
            .Select(u => new { u.NombreCompleto, u.Correo })
            .ToListAsync();
        return Ok(list);
    }

    // Empleado/ Admin: ve su propio nombre
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Correo == email);
        if (u == null) return NotFound();
        return Ok(new { u.NombreCompleto, u.Correo });
    }
}
