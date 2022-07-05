using Microsoft.EntityFrameworkCore;

namespace Boinc.Blazor.Data
{
    public class HostService
    {
        private readonly IDbContextFactory<AppDb> _dbFactory;

        public HostService(IDbContextFactory<AppDb> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<BoincHost>> GetAll()
        {
            using var db = _dbFactory.CreateDbContext();

            return await db.BoincHosts.ToListAsync();
        }

        public async Task<BoincHost> AddHost(BoincHost host)
        {
            using var db = _dbFactory.CreateDbContext();

            host = db.BoincHosts.Add(host).Entity;
            await db.SaveChangesAsync();

            BoincHostConnector.AddHost(host);

            return host;
        }
        public async Task RemoveHost(BoincHostViewModel host)
        {
            BoincHostConnector.RemoveHost(host);

            using var db = _dbFactory.CreateDbContext();
            db.BoincHosts.Remove(host);
            await db.SaveChangesAsync();
        }
    }

    public class AppDb : DbContext
    {
        public AppDb(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BoincHost>().HasKey(x => x.Id);
        }

        public DbSet<BoincHost> BoincHosts { get; set; }
    }
}
