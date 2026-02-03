using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Shared.Models.Domains
{
    public class HostedSiteDetails
    {
        public int? HostedSiteDetailsId { get; set; }
        public int? DomainId { get; set; }
        public string? HostingProvider { get; set; } = string.Empty;
        public DateTime? RenewalDate { get; set; } = DateTime.MinValue;
        public string? ServerName { get; set; } = string.Empty;
        public HostedSiteDetails() { }
    }
}
