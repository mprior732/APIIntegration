using Microsoft.EntityFrameworkCore;
using Domains.API.Data.DataModels;

namespace Domains.API.Data
{
    public class DomainDBContext : DbContext
    {
        public DomainDBContext(DbContextOptions<DomainDBContext> options) : base(options)
        {
        }
        public DbSet<DomainData> Domains { get; set; }
        public DbSet<HostedSiteData> HostedSites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //On initial DB creation, this will seed some data for testing purposes
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DomainData>().HasData(
                new DomainData { DomainId = 1, DomainName = "example.com", Status = "Active" },
                new DomainData { DomainId = 2, DomainName = "test.org", Status = "Inactive" }
            );
            modelBuilder.Entity<HostedSiteData>().HasData(
                new HostedSiteData { HostedSiteDetailsId = 1, DomainId = 1, HostingProvider = "Provider", RenewalDate = DateTime.UtcNow.AddMonths(6), ServerName = "AZ102" },
                new HostedSiteData { HostedSiteDetailsId = 2, DomainId = 2, HostingProvider = "Provider2", RenewalDate = DateTime.UtcNow.AddMonths(3), ServerName = "AZ201" }
            );
        }
        public async Task<DomainData?> GetDomainAsync(int? id, string? domainName)
        {
            if (id != 0)
            {
                return await Domains.FirstOrDefaultAsync(d => d.DomainId == id);
            }
            else if (!string.IsNullOrWhiteSpace(domainName))
            {
                return await Domains.FirstOrDefaultAsync(d => d.DomainName == domainName);
            }
            return null;
        }

        public async Task<List<DomainData>> GetAllDomainsAsync()
        {
            return await Domains.ToListAsync();
        }

        public async Task<DomainData> SaveDomainAsync(DomainData domain)
        {
            await Domains.AddAsync(domain);
            await SaveChangesAsync();
            return await GetDomainAsync(0, domain.DomainName);
        }

        public async Task<bool> UpdateDomainAsync(DomainData domain)
        {
            Domains.Update(domain);
            var result = await SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteDomainAsync(DomainData domain)
        {
            Domains.Remove(domain);
            var result = await SaveChangesAsync();
            return result > 0;
        }

        public async Task<HostedSiteData?> GetHostedSiteByDomainIdAsync(int domainId)
        {
            return await HostedSites.FirstOrDefaultAsync(h => h.DomainId == domainId);
        }

        public async Task<List<HostedSiteData>> GetHostedSitesByDomainIdListAsync(List<int> domainIdList)
        {
            return await HostedSites.Where(h => domainIdList.Contains(h.DomainId ?? 0)).ToListAsync();
        }

        public async Task<bool> SaveHostedSiteAsync(HostedSiteData hostedSite)
        {
            await HostedSites.AddAsync(hostedSite);
            var result = await SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateHostedSiteAsync(HostedSiteData hostedSite)
        {
            HostedSites.Update(hostedSite);
            var result = await SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteHostedSiteAsync(HostedSiteData hostedSite)
        {
            HostedSites.Remove(hostedSite);
            var result = await SaveChangesAsync();
            return result > 0;
        }
    }
}
