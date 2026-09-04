using Microsoft.EntityFrameworkCore;
using dotnet_sample.Services;

namespace dotnet_sample.Data;

// DbContext mínimo só pra demonstrar o uso de IDbContextFactory<T> em um
// background job. Usa o provider InMemory (sem banco real instalado).
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
}
