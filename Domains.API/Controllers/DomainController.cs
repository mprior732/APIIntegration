using Domains.API.Data.DataModels;
using Microsoft.AspNetCore.Mvc;
using Models.Shared.Models.Domains;

namespace Domains.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DomainController : Controller
    {
        Managers.DomainManager _domainManager;
        Helpers.SettingsHelper _settings;
        public DomainController(Managers.DomainManager manager, Helpers.SettingsHelper settings)
        {
            _domainManager = manager;
            _settings = settings;
        }

        private bool IsValidApiKey()
        {
            if (Request.Headers.TryGetValue("X-API-Key", out var headerKey))
            {
                return headerKey == _settings._apiKey;
            }

            return false;
        }

        [HttpGet("{domainNameOrId}")]
        public async Task<ActionResult<Domain>> GetOne(string domainNameOrID)
        {
            if (!IsValidApiKey())
            {
                return Unauthorized("Invalid or missing API Key");
            }
            var domain = await _domainManager.GetOne(domainNameOrID);
            if (domain == null)
            {
                return NotFound($"Domain with identifier '{domainNameOrID}' not found.");
            }
            return Ok(domain);
        }

        [HttpGet()]
        public async Task<ActionResult<List<Domain>>> GetAll()
        {
            if (!IsValidApiKey())
            {
                return Unauthorized("Invalid or missing API Key");
            }
            var domains = await _domainManager.GetAll();
            return Ok(domains);
        }

        [HttpPost()]
        public async Task<ActionResult<Domain>> DomainSave([FromBody] Domain domain)
        {
            if (!IsValidApiKey())
            {
                return Unauthorized("Invalid or missing API Key");
            }
            var savedDomain = await _domainManager.DomainSave(domain);
            return Ok(savedDomain);
        }

        [HttpPut()]
        public async Task<ActionResult<Domain>> DomainUpdate([FromBody] Domain domain)
        {
            if (!IsValidApiKey())
            {
                return Unauthorized("Invalid or missing API Key");
            }
            var updatedDomain = await _domainManager.DomainUpdate(domain);
            return Ok(updatedDomain);
        }

        [HttpDelete("{domainID}")]
        public async Task<Domain> DomainDelete(int domainID)
            => await _domainManager.DomainDelete(domainID);
    }
        
}
