namespace SecureApi.Models;

public class Empleado
{
    public int ID_Empleado { get; set; }
    public int ID_Usuario { get; set; }
    public string? Puesto { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;

    public Usuario? Usuario { get; set; }
}
