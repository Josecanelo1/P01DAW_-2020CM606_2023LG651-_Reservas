using Microsoft.EntityFrameworkCore;

namespace P01_2020CM606_2023LG651.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<EspacioParque> EspaciosParque { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
    }
}
