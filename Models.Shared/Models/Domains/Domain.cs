using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Shared.Models.Domains
{
    public class Domain : ModelBase
    {
        public int? DomainId { get; set; }
        public string? DomainName { get; set; } = string.Empty;
        public string? Status { get; set; } = string.Empty;
        public HostedSiteDetails? HostedSiteDetails { get; set; } = new HostedSiteDetails();

        public Domain() { }

        public Domain(bool success, string message) : base(success, message) { }
    }
}
