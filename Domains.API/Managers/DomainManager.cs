using Models.Shared.Models.Domains;
using Domains.API.Data;
using Domains.API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Domains.API.Managers
{
    public class DomainManager
    {
        private readonly DomainDBContext _context;
        private readonly ValidationHelper _validationHelper;
        public DomainManager(DomainDBContext context, ValidationHelper validationHelper)
        {
            _context = context;
            _validationHelper = validationHelper;
        }
        /// <summary>
        /// Retrieves a single domain by its name or ID.
        /// </summary>
        /// <param name="DomainNameOrID"></param>
        /// <returns></returns>
        public async Task<Domain> GetOne([FromQuery] string DomainNameOrID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DomainNameOrID))
                {
                    return new Domain
                    {
                        Success = false,
                        Message = "DomainNameOrID cannot be null or empty."
                    };
                }

                string domainName;
                if (int.TryParse(DomainNameOrID, out int domainID))
                {
                    domainName = string.Empty;
                }
                else
                {
                    domainName = DomainNameOrID;
                    domainID = 0;
                }

                Data.DataModels.DomainData? domainData = await _context.GetDomainAsync(domainID, domainName);
                if (domainData == null)
                {
                    return new Domain
                    {
                        Success = false,
                        Message = "Domain not found."
                    };
                }

                Domain domain = new Domain
                {
                    Success = true,
                    DomainId = domainData.DomainId,
                    DomainName = domainData.DomainName,
                    Status = domainData.Status
                };

                Data.DataModels.HostedSiteData? hostedSiteData = await _context.GetHostedSiteByDomainIdAsync(domainData.DomainId ?? 0);

                if (hostedSiteData != null)
                {
                    domain.HostedSiteDetails = new HostedSiteDetails
                    {
                        HostedSiteDetailsId = hostedSiteData.HostedSiteDetailsId,
                        DomainId = hostedSiteData.DomainId,
                        HostingProvider = hostedSiteData.HostingProvider,
                        RenewalDate = hostedSiteData.RenewalDate,
                        ServerName = hostedSiteData.ServerName
                    };
                }

                return domain;
            }
            catch (Exception ex)
            {
                return new Domain
                {
                    Success = false,
                    Message = $"Exception thrown in GetOne: {ex.Message}"
                };
            }
        }

        public async Task<List<Domain>> GetAll()
        {
            string methodName = nameof(GetAll);
            List<Domain> domains = new List<Domain>();
            try
            {
                List<Data.DataModels.DomainData> domainDataList = 
                    await _context.GetAllDomainsAsync();

                List<int> domainIdList = domainDataList
                    .Where(d => d.DomainId.HasValue)
                    .Select(d => d.DomainId.Value)
                    .ToList();

                List<Data.DataModels.HostedSiteData> hostedSiteDataList = 
                    await _context.GetHostedSitesByDomainIdListAsync(domainIdList);

                foreach (var domainData in domainDataList)
                {
                    Domain domain = new Domain
                    {
                        Success = true,
                        DomainId = domainData.DomainId,
                        DomainName = domainData.DomainName,
                        Status = domainData.Status
                    };
                    var hostedSiteData = hostedSiteDataList
                        .FirstOrDefault(h => h.DomainId == domainData.DomainId);
                    if (hostedSiteData != null)
                    {
                        domain.HostedSiteDetails = new HostedSiteDetails
                        {
                            HostedSiteDetailsId = hostedSiteData.HostedSiteDetailsId,
                            DomainId = hostedSiteData.DomainId,
                            HostingProvider = hostedSiteData.HostingProvider,
                            RenewalDate = hostedSiteData.RenewalDate,
                            ServerName = hostedSiteData.ServerName
                        };
                    }

                    domains.Add(domain);
                }
                    return domains;
            }
            catch (Exception ex)
            {
                domains.Add(new Domain
                {
                    Success = false,
                    Message = $"Exception thrown in {methodName}: {ex.Message}"
                });
                return domains;
            }
        }

        public async Task<Domain> DomainSave(Domain domain)
        {
            string errorMessage = string.Empty;

            //Basic validation
            _validationHelper.ValidateDomain(ref domain);
            if (!domain.Success)
            {
                return domain;
            }
            if (domain.HostedSiteDetails != null)
            {
                if (!_validationHelper.ValidateHostedSiteDetails(domain.HostedSiteDetails, ref errorMessage))
                {
                    return new Domain
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }

            try
            {
                var existingDomain = await _context.GetDomainAsync(null, domain.DomainName);
                if (existingDomain != null || existingDomain?.DomainId > 0)
                {
                    return new Domain(false, "A domain with the same name already exists.");
                }

                Data.DataModels.DomainData domainData = new Data.DataModels.DomainData
                {
                    DomainName = domain.DomainName,
                    Status = domain.Status
                };
                var saveResults = await _context.SaveDomainAsync(domainData);
                if (saveResults == null || saveResults.DomainId <= 0)
                {
                    return new Domain
                    {
                        Success = false,
                        Message = "Failed to save domain."
                    };
                }

                if (domain.HostedSiteDetails != null)
                {
                    var existingHostedSite = await _context.GetHostedSiteByDomainIdAsync(saveResults.DomainId.Value);
                    if (existingHostedSite != null)
                    {
                        errorMessage = "Hosted site details already exist for this domain. Skipping save.";
                    }
                    else
                    {
                        Data.DataModels.HostedSiteData hostedSiteData = new Data.DataModels.HostedSiteData
                        {
                            DomainId = saveResults.DomainId,
                            HostingProvider = domain.HostedSiteDetails.HostingProvider,
                            RenewalDate = domain.HostedSiteDetails.RenewalDate,
                            ServerName = domain.HostedSiteDetails.ServerName
                        };
                        if (!await _context.SaveHostedSiteAsync(hostedSiteData))
                        {
                            errorMessage = "Failed to save hosted site details.";
                        }
                    }
                }

                return new Domain
                {
                    Success = true,
                    DomainId = saveResults.DomainId,
                    DomainName = saveResults.DomainName,
                    Status = saveResults.Status,
                    HostedSiteDetails = domain.HostedSiteDetails,
                    Message = $"Domain saved successfully. {errorMessage}".Trim()
                };
            }
            catch (Exception ex)
            {
                return new Domain
                {
                    Success = false,
                    Message = $"Exception thrown in DomainSave: {ex.Message}"
                };
            }
        }

        public async Task<Domain> DomainUpdate(Domain domain)
        {
            string message = string.Empty;

            //Basic validation
            _validationHelper.ValidateDomain(ref domain);
            if (!domain.Success)
            {
                return domain;
            }
            if (domain.HostedSiteDetails != null)
            {
                if (!_validationHelper.ValidateHostedSiteDetails(domain.HostedSiteDetails, ref message))
                {
                    return new Domain
                    {
                        Success = false,
                        Message = message
                    };
                }
            }

            try
            {
                var existingDomain = await _context.GetDomainAsync(domain.DomainId, null);
                if (existingDomain == null || existingDomain?.DomainId == null || existingDomain?.DomainId == 0)
                {
                    return new Domain(false, $"No domain exists with Domain Id: {domain.DomainId}.");
                }

                existingDomain.DomainName = domain.DomainName;
                existingDomain.Status = domain.Status;

                if (!await _context.UpdateDomainAsync(existingDomain))
                {
                    return new Domain
                    {
                        Success = false,
                        Message = "Failed to save domain."
                    };
                }

                if (domain.HostedSiteDetails != null)
                {
                    Data.DataModels.HostedSiteData hostedSiteData = new Data.DataModels.HostedSiteData
                    {
                        DomainId = domain.DomainId,
                        HostingProvider = domain.HostedSiteDetails.HostingProvider,
                        RenewalDate = domain.HostedSiteDetails.RenewalDate,
                        ServerName = domain.HostedSiteDetails.ServerName
                    };

                    var existingHostedSite = await _context.GetHostedSiteByDomainIdAsync(domain.DomainId.Value);
                    if (existingHostedSite != null)
                    {
                        hostedSiteData.HostedSiteDetailsId = existingHostedSite.HostedSiteDetailsId;
                        message = !await _context.UpdateHostedSiteAsync(hostedSiteData)
                            ? "Failed to update hosted site details."
                            : "Hosted site details updated successfully.";
                    }
                    else
                    {
                        message = !await _context.SaveHostedSiteAsync(hostedSiteData)
                            ? "Failed to save hosted site details."
                            : "Hosted site details saved successfully.";
                    }
                }
                return new Domain
                {
                    Success = true,
                    DomainId = domain.DomainId,
                    DomainName = domain.DomainName,
                    Status = domain.Status,
                    HostedSiteDetails = domain.HostedSiteDetails,
                    Message = $"Domain updated successfully. {message}".Trim()
                };
            }
            catch (Exception ex)
            {
                return new Domain(false, ex.Message);
            }
        }
    }
}
