using Microsoft.EntityFrameworkCore;
using SecureApi.Models;

namespace SecureApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empleado> Empleados => Set<Empleado>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.ID_Usuario);
            e.Property(x => x.Correo).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Correo).HasDatabaseName("IX_Usuarios_Correo");
        });

        mb.Entity<Empleado>(e =>
        {
            e.ToTable("Empleados");
            e.HasKey(x => x.ID_Empleado);
            e.HasOne(x => x.Usuario)
             .WithMany(u => u.Empleados!)
             .HasForeignKey(x => x.ID_Usuario);
        });
    }
}
