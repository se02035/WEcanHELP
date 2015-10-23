using System.Data.Entity;

namespace DataLayer
{
    public class WeCanHelpContext : DbContext
    {
        public WeCanHelpContext() : base("name=WeCanHelpContext")
        {
        }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Application> Applications { get; set; }
    }
}