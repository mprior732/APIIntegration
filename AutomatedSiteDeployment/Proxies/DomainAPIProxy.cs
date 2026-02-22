using AutomatedSiteDeployment.Helpers;
using Models.Shared.Models.Domains;
using System.Net.Http.Json;
using System.Runtime;

namespace AutomatedSiteDeployment.Proxies
{
    internal class DomainAPIProxy
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsHelper _settings;
        public DomainAPIProxy(HttpClient client, SettingsHelper settings)
        {
            _httpClient = client;
            _settings = settings;
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings._apiKey);
        }

        public async Task<Domain?> GetDomainAsync(string domainNameOrId)
        {
            try
            {
                var url = $"{_settings._domainsAPIBase}/api/domain/{domainNameOrId}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new Domain(false, "Response returned a non-success status code.");
                }
                Domain? domain = await response.Content.ReadFromJsonAsync<Domain>();
                return domain;
            }
            catch (Exception ex)
            {
                return new Domain(false, ex.Message);
            }
        }

        public async Task<List<Domain?>?> GetAllDomainsAsync()
        {
            try
            {
                var url = $"{_settings._domainsAPIBase}/api/domain/";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<Domain?>() { new Domain(false, "Response returned a non-success status code.") };
                }
                List<Domain?> domains = await response.Content.ReadFromJsonAsync<List<Domain>>();
                return domains;
            }
            catch (Exception ex)
            {
                return new List<Domain?>() { new Domain(false, ex.Message) };
            }
        }

        public async Task<Domain?> CreateDomainAsync(Domain domain)
        {
            try
            {
                var url = $"{_settings._domainsAPIBase}/api/domain/";
                var response = await _httpClient.PostAsJsonAsync(url, domain);
                if (!response.IsSuccessStatusCode)
                {
                    return new Domain(false, "Response returned a non-success status code.");
                }
                Domain? savedDomain = await response.Content.ReadFromJsonAsync<Domain>();
                return savedDomain;
            }
            catch (Exception ex)
            {
                return new Domain(false, ex.Message);
            }
        }

        public async Task<Domain?> UpdateDomainAsync(Domain domain)
        {
            try
            {
                var url = $"{_settings._domainsAPIBase}/api/domain/";
                var response = await _httpClient.PutAsJsonAsync(url, domain);
                if (!response.IsSuccessStatusCode)
                {
                    return new Domain(false, "Response returned a non-success status code.");
                }
                Domain? updatedDomain = await response.Content.ReadFromJsonAsync<Domain>();
                return updatedDomain;
            }
            catch (Exception ex)
            {
                return new Domain(false, ex.Message);
            }
        }

        public async Task<Domain?> DeleteDomainAsync(int domainId)
        {
            try
            {
                var url = $"{_settings._domainsAPIBase}/api/domain/{domainId}";
                var response = await _httpClient.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new Domain(false, "Response returned a non-success status code.");
                }
                Domain? deleteResponse = await response.Content.ReadFromJsonAsync<Domain>();
                return deleteResponse;
            }
            catch (Exception ex)
            {
                return new Domain(false, ex.Message);
            }
        }
    }
}
