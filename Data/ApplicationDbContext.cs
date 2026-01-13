using Microsoft.EntityFrameworkCore;
using BuscaYa.Models.Entities;

namespace BuscaYa.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Sistema
    public DbSet<Usuario> Usuarios { get; set; }

    // Tiendas y Productos
    public DbSet<Tienda> Tiendas { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }

    // Analytics
    public DbSet<Estadistica> Estadisticas { get; set; }

    // Cliente
    public DbSet<Favorito> Favoritos { get; set; }
    public DbSet<HistorialBusqueda> HistorialBusquedas { get; set; }
    public DbSet<DireccionGuardada> DireccionesGuardadas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Contrasena).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasIndex(e => e.NombreUsuario).IsUnique();
            
            entity.HasOne(e => e.Tienda)
                .WithOne(t => t.Usuario)
                .HasForeignKey<Tienda>(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Tienda
        modelBuilder.Entity<Tienda>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.WhatsApp).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Direccion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Latitud).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(11,8)");
            entity.Property(e => e.Ciudad).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Departamento).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DiasAtencion).HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.FotoUrl).HasMaxLength(500);
            entity.Property(e => e.Plan).IsRequired().HasMaxLength(20).HasDefaultValue("Free");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            
            entity.HasIndex(e => e.Ciudad);
            entity.HasIndex(e => e.Activo);
        });

        // Producto
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Moneda).IsRequired().HasMaxLength(10).HasDefaultValue("C$");
            entity.Property(e => e.FotoUrl).HasMaxLength(500);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            
            entity.HasOne(e => e.Tienda)
                .WithMany(t => t.Productos)
                .HasForeignKey(e => e.TiendaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.TiendaId);
            entity.HasIndex(e => e.CategoriaId);
            entity.HasIndex(e => e.Activo);
            entity.HasIndex(e => e.Nombre); // Para full-text search
        });

        // Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icono).HasMaxLength(50);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Orden).HasDefaultValue(0);
            
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // Estadistica
        modelBuilder.Entity<Estadistica>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TipoEvento).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductoBuscado).HasMaxLength(200);
            entity.Property(e => e.DatosAdicionales).HasMaxLength(1000);
            
            entity.HasOne(e => e.Tienda)
                .WithMany(t => t.Estadisticas)
                .HasForeignKey(e => e.TiendaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.TiendaId, e.Fecha });
            entity.HasIndex(e => e.TipoEvento);
        });

        // Favorito
        modelBuilder.Entity<Favorito>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Favoritos)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Tienda)
                .WithMany(t => t.Favoritos)
                .HasForeignKey(e => e.TiendaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Producto)
                .WithMany(p => p.Favoritos)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UsuarioId, e.TiendaId, e.ProductoId });
        });

        // HistorialBusqueda
        modelBuilder.Entity<HistorialBusqueda>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Termino).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Latitud).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(11,8)");
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.HistorialBusquedas)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => e.Fecha);
        });

        // DireccionGuardada
        modelBuilder.Entity<DireccionGuardada>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Direccion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Latitud).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(11,8)");
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.DireccionesGuardadas)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.UsuarioId);
        });
    }
}
