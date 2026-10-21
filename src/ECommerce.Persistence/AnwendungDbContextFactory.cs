using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Persistence;

public sealed class AnwendungDbContextFactory : IDesignTimeDbContextFactory<AnwendungDbContext>
{
    public AnwendungDbContext CreateDbContext(string[] args)
    {
        var optionen = new DbContextOptionsBuilder<AnwendungDbContext>()
            .UseSqlite("Data Source=ecommerce.db")
            .Options;
        return new AnwendungDbContext(optionen);
    }
}
