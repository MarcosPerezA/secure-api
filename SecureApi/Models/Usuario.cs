namespace SecureApi.Models;

public class Usuario
{
    public int ID_Usuario { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string Contrasena { get; set; } = null!;
    public string Rol { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public ICollection<Empleado>? Empleados { get; set; }
}
