using AutomatedSiteDeployment.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Shared.Models.Domains;
using AutomatedSiteDeployment.Proxies;
using AutomatedSiteDeployment.Agents;

namespace AutomatedSiteDeployment.Managers
{
    internal class ServiceManager
    {
        SettingsHelper _settings;
        DomainAPIProxy _proxy;
        public ServiceManager(HttpClient client, SettingsHelper settings)
        {
            _settings = settings;
            _proxy = new DomainAPIProxy(client, settings);
        }

        public async Task GetAllDoamins()
        {
            List<Domain?>? domains = await _proxy.GetAllDomainsAsync();
            if (domains == null || domains.Count == 0)
            {
                Console.WriteLine("No domains found.");
                return;
            }
            if (!domains[0].Success)
            {
                Console.WriteLine($"Error retrieving domains: {domains[0].Message}");
                return;
            }

            foreach (var domain in domains)
            {
                bool hasHostedSite = domain.HostedSiteDetails != null && domain.HostedSiteDetails.HostedSiteDetailsId > 0;
                Console.WriteLine($"Domain ID: {domain.DomainId}, Domain Name: {domain.DomainName}, Status: {domain.Status}, HasHostedSite: {hasHostedSite}");
            }
        }

        public async Task SaveNewDomain()
        {
            Console.WriteLine("Enter desired Domain Name:");
            var domainName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(domainName))
            {
                Console.WriteLine("Invalid Domain Name. Operation cancelled.");
                return;
            }

            Domain newDomain = new Domain
            {
                DomainName = domainName,
                Status = "Active",
                HostedSiteDetails = null
            };
            Domain? createdDomain = await _proxy.CreateDomainAsync(newDomain);
            if (createdDomain == null || !createdDomain.Success)
            {
                string? errorMessage = createdDomain == null ? "No response from server." : createdDomain?.Message;
                Console.WriteLine($"Failed to create domain: {errorMessage}");
                return;
            }
            Console.WriteLine($"Domain created successfully with ID: {createdDomain.DomainId} and Name: {createdDomain.DomainName}");
        }

        public async Task<Domain> UpdateDomain(Domain domain)
        {
            // Generate Hosted Site Details
            int rngNum = Random.Shared.Next(0, 2);
            HostedSiteDetails details = new HostedSiteDetails
            {
                DomainId = domain.DomainId,
                HostingProvider = rngNum == 0 ? "ProviderA" : "ProviderB",
                RenewalDate = DateTime.UtcNow.AddYears(1),
                ServerName = rngNum == 0 ? "AZ102" : "AZ201",
            };
            domain.HostedSiteDetails = details;

            Domain? updatedDomain = await _proxy.UpdateDomainAsync(domain);
            if (updatedDomain == null || !updatedDomain.Success)
            {
                string? errorMessage = updatedDomain == null ? "No response from server." : updatedDomain?.Message;
                Console.WriteLine($"Failed to update domain: {errorMessage}");
                return domain;
            }
            Console.WriteLine($"Domain updated successfully with HostedSiteId: {updatedDomain.HostedSiteDetails?.HostedSiteDetailsId}");
            return updatedDomain;
        }

        public async Task DeleteDomain()
        {
            Console.WriteLine("Enter Domain ID to delete:");
            try
            {
                var domainID = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(domainID) && int.TryParse(domainID, out int ID))
                {
                    Domain? deletedDomian = await _proxy.DeleteDomainAsync(ID);
                    if (deletedDomian == null)
                    {
                        Console.WriteLine("No response from server. Failed to delete domain.");
                        return;
                    }
                    else if (!deletedDomian.Success)
                    {
                        Console.WriteLine($"Failed to delete domain: {deletedDomian.Message}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Domain with ID {ID} deleted successfully.");
                        return;
                    }

                }
                else
                {
                    Console.WriteLine("Please enter a valid ID");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
        public async Task DeployHostedSite()
        {
            bool domainFound = false;
            Domain? domain = new Domain();
            Console.WriteLine("Enter Domain Name or ID:");
            try
            {
                do
                {
                    var nameOrId = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(nameOrId))
                    {
                        Console.WriteLine("Please enter a valid Domain Name or ID:");
                        continue;
                    }
                    if (nameOrId.ToLower() == "x")
                    {
                        return;
                    }

                    domain = await _proxy.GetDomainAsync(nameOrId);
                    if (domain == null || !domain.Success)
                    {
                        Console.WriteLine("Domain not found. Please enter a valid Domain Name or ID");
                        Console.WriteLine("Or enter \"X\" to return to the menu:");
                        continue;
                    }
                    else if (domain.HostedSiteDetails == null || domain.HostedSiteDetails.HostedSiteDetailsId == null)
                    {
                        Console.WriteLine("Domain found but no hosted site details exist.");
                        Console.WriteLine("Generating hosted site...");
                        Domain updatedDomain = await UpdateDomain(domain);

                        if (!domain.Success)
                        {
                            Console.WriteLine($"Failed to generate hosted site details: {domain.Message}");
                            Console.WriteLine("Please try again");
                            Console.WriteLine("Or enter \"X\" to return to the menu:");
                            continue;
                        }

                        Console.WriteLine("Adding hosted site to staging server.");
                        FileSystemAgent stagingAgent = new FileSystemAgent(_settings._stgUsername, _settings._stgPassword, "AZstg");
                        stagingAgent.CreateDirectory(Path.Combine(_settings._stgPath, updatedDomain.DomainName));
                        await Task.Delay(2000);
                        stagingAgent.Stop();

                        Console.WriteLine("Hosted site details have been generated. Continue with current domain? [y/n]");
                        var response = Console.ReadLine();
                        if (response == null || response.ToLower() != "y")
                        {
                            continue;
                        }
                    }
                    domainFound = true;

                } while (!domainFound);

                Console.WriteLine($"Domain {domain.DomainName} with ID {domain.DomainId} found.");
                Console.WriteLine("Proceeding with deployment...");

                SiteDeploymentManager siteDeployment = new SiteDeploymentManager(domain, _settings);

                await siteDeployment.StartDeployment();

                if (siteDeployment.deploySuccess)
                {
                    Console.WriteLine($"Deployment was successful!");
                }
                else
                {
                    Console.WriteLine($"Deployment failed: {siteDeployment.message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        
    }
}
